using System.Net.Http.Json;

namespace RagLib.Embeddings
{
    public interface IEmbeddingClient
    {
        Task<float[]> EmbedAsync(string text, CancellationToken ct);
    }

    public sealed class OllamaEmbeddingClient : IEmbeddingClient
    {
        private readonly HttpClient _http;
        private readonly string _model;

        public OllamaEmbeddingClient(HttpClient http, string model)
        {
            _http = http;
            _model = model;
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
        {
            using var resp = await _http.PostAsJsonAsync("/api/embeddings", new { model = _model, prompt = text }, ct);
            resp.EnsureSuccessStatusCode();
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var emb = doc.RootElement.GetProperty("embedding");
            var result = new float[emb.GetArrayLength()];
            int i = 0;
            foreach (var v in emb.EnumerateArray()) result[i++] = v.GetSingle();
            return result;
        }
    }
}
