using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RagLib.Embeddings;
using RagLib.Models;
using RagLib.Storage;
using RagLib.TextExtractors;

namespace RagLib.DI
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers text extraction, Ollama embeddings and the pgvector store.
        /// Values come from configuration; the legacy environment variables still win when set:
        ///   VectorStore:ConnectionString  (env POSTGRES_CS)
        ///   Ollama:BaseUrl                (env OLLAMA_BASE)
        ///   Ollama:EmbedModel             (env EMBED_MODEL, default bge-m3)
        ///   Ollama:EmbedDim               (env EMBED_DIM,   default 1024)
        /// Missing connection settings only fail when the dependent service is first resolved,
        /// so hosts that never touch the vector store (e.g. the Telegram bot) still start.
        /// </summary>
        public static void AddRagLibDI(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<ITextExtractorFactory, TextExtractorFactory>();

            var pg = Setting("POSTGRES_CS", config["VectorStore:ConnectionString"]);
            var ollamaBase = Setting("OLLAMA_BASE", config["Ollama:BaseUrl"]);
            var model = Setting("EMBED_MODEL", config["Ollama:EmbedModel"]) ?? "bge-m3";
            var dim = int.TryParse(Setting("EMBED_DIM", config["Ollama:EmbedDim"]), out var d) ? d : 1024;
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "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";
            var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

            services.AddSingleton(new AppConfig(pg ?? string.Empty, ollamaBase ?? string.Empty, model, dim, OpenAIKey: openAiKey, OpenAIModel: openAiModel));
            services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(Require(ollamaBase, "Ollama:BaseUrl", "OLLAMA_BASE")) });
            services.AddSingleton<IEmbeddingClient, OllamaEmbeddingClient>(sp => new OllamaEmbeddingClient(sp.GetRequiredService<HttpClient>(), model));
            services.AddSingleton(sp => new VectorStore(Require(pg, "VectorStore:ConnectionString", "POSTGRES_CS"), dim));
        }

        private static string? Setting(string envVar, string? configValue)
        {
            var env = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(env)) return env;
            return string.IsNullOrWhiteSpace(configValue) ? null : configValue;
        }

        private static string Require(string? value, string configKey, string envVar) =>
            value ?? throw new InvalidOperationException(
                $"Missing setting '{configKey}' (appsettings.json) or environment variable {envVar}.");
    }
}
