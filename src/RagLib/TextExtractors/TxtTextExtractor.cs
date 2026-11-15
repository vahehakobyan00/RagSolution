namespace RagLib.TextExtractors
{
    public sealed class TxtTextExtractor : ITextExtractor
    {
        public Task<(string, Dictionary<string, object>)> ExtractAsync(string path, CancellationToken ct)
        {
            string text = File.ReadAllText(path);

            var meta = new Dictionary<string, object>
            {
                ["source"] = "txt",
                ["file"] = Path.GetFileName(path),
                ["length"] = text.Length
            };

            return Task.FromResult((text, meta));
        }

        public async Task<(string, Dictionary<string, object>)> ExtractAsync(Stream stream, string fileName, CancellationToken ct)
        {
            using var reader = new StreamReader(stream);
            string text = await reader.ReadToEndAsync(ct);

            var meta = new Dictionary<string, object>
            {
                ["source"] = "txt",
                ["file"] = fileName,
                ["length"] = text.Length
            };

            return (text, meta);
        }
    }
}
