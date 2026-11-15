namespace RagLib.TextExtractors
{
    public interface ITextExtractor
    {
        Task<(string Text, Dictionary<string, object> Meta)> ExtractAsync(string path, CancellationToken ct);
        Task<(string, Dictionary<string, object>)> ExtractAsync(Stream stream, string fileName, CancellationToken ct);
    }
}
