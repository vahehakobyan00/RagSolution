using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace RagLib.TextExtractors
{
    public sealed class DocxTextExtractorOpenXml : ITextExtractor
    {
        public Task<(string, Dictionary<string, object>)> ExtractAsync(string path, CancellationToken ct)
        {
            using var fs = File.OpenRead(path);
            using var doc = WordprocessingDocument.Open(fs, false);

            var body = doc.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                var emptyMeta = new Dictionary<string, object>
                {
                    ["source"] = "docx",
                    ["paragraphs"] = 0
                };
                return Task.FromResult((string.Empty, emptyMeta));
            }

            var paragraphs = body.Elements<Paragraph>();

            int paraCount = 0;
            var lines = new List<string>(capacity: 256);

            foreach (var p in paragraphs)
            {
                ct.ThrowIfCancellationRequested();

                // Concatenate all Text nodes in the paragraph (handles runs, bold/italic, etc.)
                var text = string.Concat(p.Descendants<Text>().Select(t => t.Text));

                // Keep only non-empty lines; remove this check if you want blank lines too
                if (!string.IsNullOrWhiteSpace(text))
                    lines.Add(text);

                paraCount++;
            }

            var joined = string.Join(Environment.NewLine + Environment.NewLine, lines);

            var meta = new Dictionary<string, object>
            {
                ["source"] = "docx",
                ["paragraphs"] = paraCount
            };

            return Task.FromResult((joined, meta));
        }

        public Task<(string, Dictionary<string, object>)> ExtractAsync(Stream stream, string fileName, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
