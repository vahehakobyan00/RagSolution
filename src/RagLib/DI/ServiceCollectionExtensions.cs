using Microsoft.Extensions.DependencyInjection;
using RagLib.Embeddings;
using RagLib.Models;
using RagLib.Storage;
using RagLib.TextExtractors;

namespace RagLib.DI
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRagLibDI(this IServiceCollection services)
        {
            services.AddScoped<ITextExtractorFactory, TextExtractorFactory>();
            var pg = Environment.GetEnvironmentVariable("POSTGRES_CS") ?? "Host=192.168.10.100;Port=5432;Database=VBTDatabase;Username=postgres;Password=123456";
            var ollamaBase = Environment.GetEnvironmentVariable("OLLAMA_BASE") ?? "http://192.168.10.100:11434";
            var model = Environment.GetEnvironmentVariable("EMBED_MODEL") ?? "bge-m3";
            var dim = int.TryParse(Environment.GetEnvironmentVariable("EMBED_DIM"), out var d) ? d : 1024;
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "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";
            var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
            var modelMaxTokens = int.TryParse(Environment.GetEnvironmentVariable("MODEL_MAX_TOKENS"), out var _mx) ? _mx : 128_000;
            var responseReserve = int.TryParse(Environment.GetEnvironmentVariable("RESPONSE_RESERVE"), out var _rr) ? _rr : 1024;

            services.AddSingleton(new AppConfig(pg, ollamaBase, model, dim, OpenAIKey: openAiKey, OpenAIModel: openAiModel));
            services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(ollamaBase) });
            services.AddSingleton<IEmbeddingClient, OllamaEmbeddingClient>(sp => new OllamaEmbeddingClient(sp.GetRequiredService<HttpClient>(), model));
            services.AddSingleton(sp => new VectorStore(pg, dim));
        }
    }
}
