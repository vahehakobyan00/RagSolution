using System.Text.RegularExpressions;

namespace RagLib.Chunking
{
    public static class TextPreprocessor
    {
        public static string CleanExtract(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var t = raw;

            // Normalize newlines
            t = t.Replace("\r\n", "\n");

            // Remove obvious page headers/footers (tweak these to your doc patterns)
            // e.g., "3[Page 4/19]" or "[Page 4/19]" lines
            t = Regex.Replace(t, @"(?m)^\s*\d*\[Page\s+\d+/\d+\]\s*$", "", RegexOptions.CultureInvariant);

            // Remove standalone titles repeated as headers (e.g., "Arca QR" on its own line)
            t = Regex.Replace(t, @"(?m)^\s*Arca\s*QR\s*$", "", RegexOptions.CultureInvariant);

            // Remove stray table-of-contents style “page dots + numbers” fragments like "154․ ... 17"
            t = Regex.Replace(t, @"(?m)^\s*\d{1,4}\s*․.*?\b\d{1,4}\s*$", "", RegexOptions.CultureInvariant);

            // Fix words glued together across line wraps: if lowercase/letter followed by newline then letter, insert space.
            t = Regex.Replace(t, @"(?<=\p{L})\n(?=\p{L})", " ");

            // Also fix words glued without space around Armenian punctuation or parentheses
            // Insert a space after Armenian full stop "։", question "՞", ellipsis …, or dot/!? if missing.
            t = Regex.Replace(t, @"(?<=[\.!\?…\u0589\u055E])(?=\p{L})", " ");
            // Insert a space before "(" if stuck to a word
            t = Regex.Replace(t, @"(?<=\p{L})\(", " (");

            // Collapse hyphenated line breaks: "սպասարկող-\nբանկին" -> "սպասարկողբանկին"
            t = Regex.Replace(t, @"-\n(?=\p{L})", "");

            // Remove duplicated lines created by wrapped duplication
            // If the same short fragment repeats on the next line, drop the duplicate.
            t = Regex.Replace(t, @"(?m)^(?<x>.{6,80})\s*\n\k<x>\s*$", "${x}", RegexOptions.CultureInvariant);

            // Collapse multiple spaces
            t = Regex.Replace(t, @"[ \t]{2,}", " ");

            // Convert single line breaks inside a paragraph into spaces, but keep blank lines as paragraph breaks.
            // First mark paragraph breaks:
            t = Regex.Replace(t, @"\n\s*\n", "\n\n");      // normalize blank lines
                                                           // Then turn single newlines (not a blank-line) into spaces:
            t = Regex.Replace(t, @"(?<!\n)\n(?!\n)", " ");

            // Trim trailing spaces on lines; then collapse >2 blank lines into exactly 2
            t = Regex.Replace(t, @"[ \t]+\n", "\n");
            t = Regex.Replace(t, @"\n{3,}", "\n\n");

            // Optional: remove leading bullet dots that became inline noise
            t = Regex.Replace(t, @"(?m)^\s*[•●◦]\s*", "• ");

            // Final trim
            t = t.Trim();
            return t;
        }
    }

}
