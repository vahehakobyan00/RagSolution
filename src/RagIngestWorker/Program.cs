using RagLib.Embeddings;
using RagLib.Storage;
using RagLib.TextExtractors;
using RagLib.Chunking;
using RagLib.Models;

if (args.Length < 3)
{
    Console.WriteLine("Usage: RagIngestWorker <pdf_or_docx_path> <doc_id> \"Host=...;Database=...;Username=...;Password=...\"");
    return;
}

var path = args[0];
var docId = args[1];
var cs = args[2];

var cfg = new AppConfig(
    Postgres: cs,
    OllamaBase: Environment.GetEnvironmentVariable("OLLAMA_BASE") ?? "http://192.168.10.100:11434",
    OllamaModel: Environment.GetEnvironmentVariable("EMBED_MODEL") ?? "bge-m3",
    EmbeddingDim: int.TryParse(Environment.GetEnvironmentVariable("EMBED_DIM"), out var d) ? d : 1024
);

var http = new HttpClient { BaseAddress = new Uri(cfg.OllamaBase) };
var emb = new OllamaEmbeddingClient(http, cfg.OllamaModel);
var store = new VectorStore(cfg.Postgres, cfg.EmbeddingDim);

ITextExtractor extractor = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".pdf" => new PdfTextExtractor(),
    ".docx" => new DocxTextExtractor(),
    _ => throw new NotSupportedException("Only .pdf and .docx supported in this worker.")
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

await store.EnsureSchemaAsync(cts.Token);

var (text, baseMeta) = await extractor.ExtractAsync(path, cts.Token);

var chunks = TokenChunker.Chunk(text, 800, 120, "cl100k_base")
                         .Select((c, i) => (i, c.Text, c.FromToken, c.ToToken))
                         .ToList();

var rows = new List<(int, string, object, float[])>(chunks.Count);
foreach (var ch in chunks)
{
    var vec = await emb.EmbedAsync(ch.Text, cts.Token);
    var meta = new Dictionary<string, object>(baseMeta)
    {
        ["from_token"] = ch.FromToken,
        ["to_token"] = ch.ToToken,
        ["file"] = Path.GetFileName(path)
    };
    rows.Add((ch.i, ch.Text, meta, vec));
}

await store.UpsertAsync(docId, rows, cts.Token);
Console.WriteLine("✅ Ingest complete.");
