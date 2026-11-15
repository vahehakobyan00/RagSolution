using Xceed.Words.NET;

namespace RagLib.TextExtractors
{
    public sealed class DocxTextExtractor : ITextExtractor
    {
        public Task<(string, Dictionary<string, object>)> ExtractAsync(string path, CancellationToken ct)
        {
            using var doc = DocX.Load(path);
            var text = doc.Text ?? string.Empty;
            var meta = new Dictionary<string, object>
            {
                ["source"] = "docx",
                ["paragraphs"] = doc.Paragraphs?.Count ?? 0
            };
            return Task.FromResult((text, meta));
        }

        public Task<(string, Dictionary<string, object>)> ExtractAsync(Stream stream, string fileName, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
