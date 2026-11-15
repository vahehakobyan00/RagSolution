using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace RagLib.Embeddings
{
    internal class TeiEmbeddingClient : IEmbeddingClient
    {
        private readonly HttpClient _http;
        public TeiEmbeddingClient(HttpClient http) => _http = http;

        // TEI ожидает { "inputs": ["text1","text2", ...] } и возвращает { "embeddings": [[...],[...]] }
        public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
        {
            var payload = new { inputs = new[] { text } };
            using var resp = await _http.PostAsJsonAsync("/embed", payload, ct);
            resp.EnsureSuccessStatusCode();

            using var doc = await System.Text.Json.JsonDocument.ParseAsync(
                await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var arr = doc.RootElement.GetProperty("embeddings")[0];
            var v = new float[arr.GetArrayLength()];
            int i = 0;
            foreach (var x in arr.EnumerateArray()) v[i++] = x.GetSingle();
            return v;
        }
    }
}