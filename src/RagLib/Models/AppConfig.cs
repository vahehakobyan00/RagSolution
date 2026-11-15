namespace RagLib.Models
{
    public record AppConfig(
        string Postgres,
        string OllamaBase,
        string OllamaModel,
        int EmbeddingDim,
        int ChunkMaxTokens = 800,
        int ChunkOverlapTokens = 120,
        string TokenizerEncoding = "cl100k_base",
        string? OpenAIKey = null,
        string OpenAIModel = "gpt-4o-mini"
    );
}
