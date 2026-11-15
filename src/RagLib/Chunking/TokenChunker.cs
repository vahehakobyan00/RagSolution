using System.Text.RegularExpressions;
using SharpToken;

namespace RagLib.Chunking
{
    public static class TokenChunker
    {
        /// <summary>
        /// Sentence- and paragraph-aware chunking with smart snapping.
        /// - Packs whole sentences up to maxTokens.
        /// - Chunks END at sentence boundaries; next chunk STARTS at paragraph start if that paragraph <= maxTokens.
        /// - For LONG paragraphs, START at nearest sentence at/after the overlap (still not mid-sentence).
        /// - If a single sentence > maxTokens, it is hard-split by tokens.
        /// - Optional soft splits for mega paragraphs/sentences (bullets, single newlines, ; : — ,).
        /// </summary>
        public static IEnumerable<(string Text, int FromToken, int ToToken)>
            Chunk(
                string text,
                int maxTokens,
                int overlapTokens,
                string encodingName,
                bool enableSoftParagraphSplits = true)
        {
            text = TextPreprocessor.CleanExtract(text);
            var enc = GptEncoding.GetEncoding(encodingName);

            // 0) Paragraphs: separated by at least one blank line (keep the blank inside block to preserve layout)
            var paragraphPattern = @"(?s).*?(?:\r?\n\s*\r?\n|$)";
            var paragraphMatches = Regex.Matches(text ?? string.Empty, paragraphPattern);
            var paragraphs = paragraphMatches
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            // If nothing was detected as paragraphs, treat whole text as a single paragraph
            if (paragraphs.Count == 0 && !string.IsNullOrWhiteSpace(text))
                paragraphs.Add(text);

            // 1) Robust sentence detection (no inline flags; we pass RegexOptions instead):
            //    - ASCII: . ! ?
            //    - Unicode: … (ellipsis), Armenian: ։ (U+0589), ՞ (U+055E)
            //    - Allow a closing quote/paren after the terminator
            //    - Fallback tail at EOF
            var sentencePattern = @"
                .*?                                         # minimal run
                (?:                                         # sentence terminator group
                    (?<=[\.!\?…\u0589\u055E])               # core terminators
                    (?:[""\)'\]\u00BB\u2019\u201D])?        # optional closing ” ’ ) ] »
                    (?:\s+|$)                                # whitespace or end
                )
                |
                .+$                                          # tail if no terminator
            ";

            var sentenceRegexOptions =
                RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace;

            var sentences = new List<string>();
            var sentenceParaIndex = new List<int>();             // sentence -> paragraph index
            var firstSentenceIndexOfParagraph = new List<int>(); // paragraph -> first sentence index

            for (int p = 0; p < paragraphs.Count; p++)
            {
                var para = paragraphs[p];

                // First pass: sentence split
                var sm = Regex.Matches(para, sentencePattern, sentenceRegexOptions);
                var paraSentences = sm.Cast<Match>()
                                      .Select(m => m.Value)
                                      .Select(s => s.Trim())
                                      .Where(s => s.Length > 0)
                                      .ToList();

                // If regex found nothing (e.g., PDF with no punctuation), treat whole paragraph as one sentence
                if (paraSentences.Count == 0 && !string.IsNullOrWhiteSpace(para))
                    paraSentences.Add(para.Trim());

                // Paragraph length in tokens (used to decide “long paragraph”)
                int paraTokLen = enc.Encode(para).Count;

                // Optional: soften very long paragraphs or very long "sentences"
                if (enableSoftParagraphSplits && paraTokLen > maxTokens)
                {
                    var softened = new List<string>();
                    foreach (var s in paraSentences)
                    {
                        var sTok = enc.Encode(s).Count;
                        softened.AddRange(sTok > maxTokens ? SoftSplit(s) : new[] { s });
                    }
                    if (softened.Count > 0)
                        paraSentences = softened;
                }

                firstSentenceIndexOfParagraph.Add(sentences.Count);
                foreach (var s in paraSentences)
                {
                    sentences.Add(s);
                    sentenceParaIndex.Add(p);
                }
            }

            // If still no sentences (empty text), just return nothing
            if (sentences.Count == 0)
                yield break;

            // 2) Precompute token starts for each sentence
            var sentTokenCounts = new int[sentences.Count];
            var sentTokenStarts = new int[sentences.Count + 1];
            for (int i = 0; i < sentences.Count; i++)
            {
                sentTokenCounts[i] = enc.Encode(sentences[i]).Count;
                sentTokenStarts[i + 1] = sentTokenStarts[i] + sentTokenCounts[i];
            }
            int totalTokens = sentTokenStarts[^1];

            // 3) Chunking
            int sIdx = 0;
            while (sIdx < sentences.Count)
            {
                int startTok = sentTokenStarts[sIdx];

                // pack whole sentences
                int eIdx = sIdx;
                int used = 0;
                while (eIdx < sentences.Count && used + sentTokenCounts[eIdx] <= maxTokens)
                {
                    used += sentTokenCounts[eIdx];
                    eIdx++;
                }

                if (eIdx == sIdx)
                {
                    // One sentence is longer than maxTokens -> hard split
                    int endTok = Math.Min(startTok + maxTokens, totalTokens);
                    var chunkHard = DecodeRange(enc, text, startTok, endTok);
                    yield return (chunkHard, startTok, endTok);

                    if (endTok == totalTokens) break;

                    int desiredNextTok = Math.Max(0, endTok - overlapTokens);
                    int nextSentence = FindSentenceIndexAtOrAfter(sentTokenStarts, desiredNextTok, sIdx + 1);
                    sIdx = SnapToParagraphStartSmart(
                        nextSentence, sIdx, desiredNextTok,
                        sentenceParaIndex, firstSentenceIndexOfParagraph,
                        sentTokenStarts, maxTokens);
                    continue;
                }

                // normal chunk
                int chunkStartTok = startTok;
                int chunkEndTok = sentTokenStarts[eIdx];
                var chunkText = string.Concat(sentences.GetRange(sIdx, eIdx - sIdx));
                yield return (chunkText, chunkStartTok, chunkEndTok);

                if (eIdx >= sentences.Count) break;

                // compute next start with overlap, then snap smartly
                int desiredNext = Math.Max(0, chunkEndTok - overlapTokens);
                int nextS = FindSentenceIndexAtOrAfter(sentTokenStarts, desiredNext, sIdx + 1);
                sIdx = SnapToParagraphStartSmart(
                    nextS, sIdx, desiredNext,
                    sentenceParaIndex, firstSentenceIndexOfParagraph,
                    sentTokenStarts, maxTokens);
            }
        }

        // ---------- Helpers ----------

        private static IEnumerable<(string Text, int FromToken, int ToToken)>
            TokenSlice(GptEncoding enc, string text, int maxTokens, int overlapTokens)
        {
            var ids = enc.Encode(text ?? string.Empty);
            int i = 0;
            while (i < ids.Count)
            {
                int end = Math.Min(i + maxTokens, ids.Count);
                var slice = ids.GetRange(i, end - i);
                var chunk = enc.Decode(slice.ToList());
                yield return (chunk, i, end);
                if (end == ids.Count) yield break;
                i = Math.Max(0, end - overlapTokens);
            }
        }

        private static string DecodeRange(GptEncoding enc, string fullText, int fromToken, int toToken)
        {
            var all = enc.Encode(fullText ?? string.Empty);
            int start = Math.Min(fromToken, all.Count);
            int end = Math.Min(toToken, all.Count);
            int count = Math.Max(0, end - start);
            if (count <= 0) return string.Empty;
            var slice = all.GetRange(start, count);
            return enc.Decode(slice.ToList());
        }

        private static int FindSentenceIndexAtOrAfter(int[] sentTokenStarts, int targetTok, int minSentenceIndex)
        {
            int lo = 0, hi = sentTokenStarts.Length - 1; // last is total
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (sentTokenStarts[mid] < targetTok) lo = mid + 1;
                else hi = mid;
            }
            int idx = Math.Max(lo, minSentenceIndex);
            if (idx >= sentTokenStarts.Length - 1) idx = sentTokenStarts.Length - 1;
            return idx;
        }

        /// <summary>
        /// Smart start-snapping:
        /// - If target paragraph total tokens <= maxTokens -> snap to paragraph's first sentence.
        /// - Else (long paragraph) -> start at nearest sentence at/after desiredNextTok (progress ensured).
        /// </summary>
        private static int SnapToParagraphStartSmart(
            int sentenceIdx,
            int previousStartSentenceIdx,
            int desiredNextTok,
            List<int> sentenceParaIndex,
            List<int> firstSentenceIndexOfParagraph,
            int[] sentTokenStarts,
            int maxTokens)
        {
            if (sentenceIdx >= sentenceParaIndex.Count)
                return sentenceParaIndex.Count; // end

            int para = sentenceParaIndex[sentenceIdx];

            // sentences span of paragraph [first, last)
            int firstSent = firstSentenceIndexOfParagraph[para];
            int lastSentExclusive = (para + 1 < firstSentenceIndexOfParagraph.Count)
                ? firstSentenceIndexOfParagraph[para + 1]
                : sentenceParaIndex.Count;

            // paragraph token length
            int paraStartTok = sentTokenStarts[firstSent];
            int paraEndTok = sentTokenStarts[lastSentExclusive];
            int paraTokenLen = paraEndTok - paraStartTok;

            int candidate = (paraTokenLen <= maxTokens)
                ? firstSent
                : Math.Max(sentenceIdx, previousStartSentenceIdx + 1); // long paragraph: allow mid-paragraph start

            if (candidate <= previousStartSentenceIdx)
            {
                // ensure forward progress (jump to next paragraph top if possible)
                int nextPara = para + 1;
                candidate = (nextPara < firstSentenceIndexOfParagraph.Count)
                    ? firstSentenceIndexOfParagraph[nextPara]
                    : Math.Max(sentenceIdx, previousStartSentenceIdx + 1);
            }

            if (candidate > sentenceParaIndex.Count)
                candidate = sentenceParaIndex.Count;

            return candidate;
        }

        /// <summary>
        /// Softly split a very long sentence or paragraph component into smaller semantic units.
        /// Order:
        ///   1) single line breaks (not paragraph breaks)
        ///   2) bullet/ordered-list item boundaries
        ///   3) semicolons/colons/em-dash/en-dash
        ///   4) commas as last resort
        /// </summary>
        private static IEnumerable<string> SoftSplit(string sentence)
        {
            var normalized = (sentence ?? string.Empty).Replace("\r\n", "\n");

            // 1) split on single LF (exclude blank-line para breaks)
            var parts = Regex.Split(normalized, @"(?<!\n)\n(?!\n)")
                             .Select(x => x.Trim())
                             .Where(x => x.Length > 0)
                             .ToList();

            var outList = new List<string>();
            foreach (var part in parts)
            {
                // 2) bullets/ordered
                var bullets = Regex.Split(part, @"(?m)(?=^\s*(?:[-*•●◦\u2022]|\d+[\.\)]|\p{Lu}\)))")
                                   .Where(x => !string.IsNullOrWhiteSpace(x))
                                   .ToList();

                foreach (var item in bullets)
                {
                    // 3) clauses on ; : — – (keep delimiter)
                    var clauses = Regex.Split(item, @"(?<=;|:|—|–)")
                                       .Select(x => x.Trim())
                                       .Where(x => x.Length > 0)
                                       .ToList();

                    // 4) last resort: commas if still massive
                    foreach (var c in clauses)
                    {
                        if (c.Length > 800)
                        {
                            var commaParts = Regex.Split(c, @"(?<=,)")
                                                  .Select(x => x.Trim())
                                                  .Where(x => x.Length > 0);
                            outList.AddRange(commaParts);
                        }
                        else
                        {
                            outList.Add(c);
                        }
                    }
                }
            }

            return outList.Count > 0 ? outList : new[] { sentence };
        }
    }
}
