using Microsoft.AspNetCore.StaticFiles;
using OpenAI;
using OpenAI.Chat;
using RagLib.Embeddings;
using RagLib.Models;
using RagLib.Storage;
using RagLib.TextExtractors;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

var pg = Environment.GetEnvironmentVariable("POSTGRES_CS") ?? "Host=192.168.10.100;Port=5432;Database=VBTDatabase;Username=postgres;Password=123456";
var ollamaBase = Environment.GetEnvironmentVariable("OLLAMA_BASE") ?? "http://192.168.10.100:11434";
var model = Environment.GetEnvironmentVariable("EMBED_MODEL") ?? "bge-m3";
var dim = int.TryParse(Environment.GetEnvironmentVariable("EMBED_DIM"), out var d) ? d : 1024;
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "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";
var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

builder.Services.AddSingleton(new AppConfig(pg, ollamaBase, model, dim, OpenAIKey: openAiKey, OpenAIModel: openAiModel));
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(ollamaBase) });
builder.Services.AddSingleton<IEmbeddingClient, OllamaEmbeddingClient>(sp =>new OllamaEmbeddingClient(sp.GetRequiredService<HttpClient>(), model));
builder.Services.AddSingleton(sp => new VectorStore(pg, dim));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider()
});

app.UseSwagger();
app.UseSwaggerUI();

// ensure schema on startup
{
    var store = app.Services.GetRequiredService<VectorStore>();
    await store.EnsureSchemaAsync(default);
}

app.MapPost("/query", async (VectorStore store, IEmbeddingClient emb, AppConfig cfg, QueryRequest req) =>
{
    var vec = await emb.EmbedAsync(req.Query, default);
    var ctx = await store.SearchAsync(vec, req.TopK ?? 5, default);

    if (string.IsNullOrWhiteSpace(cfg.OpenAIKey))
    {
        return Results.Ok(new {
            answer = "No LLM configured. Returning top matches only.",
            matches = ctx.Select(x => new { content = x.Content, sim = x.CosSim })
        });
    }
    var client = new OpenAIClient(apiKey: cfg.OpenAIKey);
    var system = "You are a helpful assistant. Use the provided context to answer succinctly.";
    var context = string.Join("\n---\n", ctx.Select(x => x.Content));
    var prompt = $"User question:\n{req.Query}\n\nContext:\n{context}\n\nAnswer in the user's language.";

    var chatClient = client.GetChatClient(openAiModel);
    var response = await chatClient.CompleteChatAsync(
        [
            new SystemChatMessage(system),
            new UserChatMessage(prompt)
        ],
        new ChatCompletionOptions
        {
            Temperature = (float)0.2
        }
    );
    var answer = response.Value.Content[0].Text;
    return Results.Ok(new {
        answer,
        matches = ctx.Select(x => new { content = x.Content, sim = x.CosSim })
    });
});

app.MapPost("/ingest-from-file", async (VectorStore store, IEmbeddingClient emb, IngestRequest req) =>
{
    var docPath = req.Path;
    if (!string.IsNullOrEmpty(docPath))
    {
        // your custom override — keep as-is if you want
        docPath = Path.Combine("D:\\", "file.pdf");
    }

    // Prepare an output folder for extracted images
    var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    Directory.CreateDirectory(webRoot);
    var imagesRoot = Path.Combine(webRoot, "rag-images", req.DocId);
    Directory.CreateDirectory(imagesRoot);

    ITextExtractor extractor = Path.GetExtension(docPath).ToLowerInvariant() switch
    {
        ".pdf" => new PdfTextExtractor(), // ctor args were removed from RagLib
        ".docx" => new DocxTextExtractorOpenXml(),
        _ => throw new NotSupportedException("Only .pdf and .docx.")
    };

    var (text, baseMeta) = await extractor.ExtractAsync(docPath, default);

    // Expect from extractor:
    //baseMeta["page_spans"] = List<object> { { page:int, start:int, end:int } }
    //baseMeta["images"]     = List<object> { { page:int, url:string, file:string } }

    var pageSpans = new List<(int page, int start, int end)>();
    if (baseMeta.TryGetValue("page_spans", out var psObj) && psObj is IEnumerable<object> psList)
    {
        foreach (var o in psList)
        {
            if (o is IDictionary<string, object> m)
            {
                int AsInt(object v) => v is null ? 0 : Convert.ToInt32(v);
                var page = m.TryGetValue("page", out var pv) ? AsInt(pv) : 0;
                var start = m.TryGetValue("start", out var sv) ? AsInt(sv) : 0;
                var end = m.TryGetValue("end", out var ev) ? AsInt(ev) : 0;
                if (end > start) pageSpans.Add((page, start, end));
            }
        }
    }

    // images -> List<(int page, string url, string file)>
    var imagesIdx = new List<(int page, string url, string file)>();
    if (baseMeta.TryGetValue("images", out var imObj) && imObj is IEnumerable<object> imList)
    {
        foreach (var o in imList)
        {
            if (o is IDictionary<string, object> m)
            {
                int AsInt(object v) => v is null ? 0 : Convert.ToInt32(v);
                string AsStr(object v) => v?.ToString() ?? "";
                var page = m.TryGetValue("page", out var pv) ? AsInt(pv) : 0;
                var url = m.TryGetValue("url", out var uv) ? AsStr(uv) : "";
                var file = m.TryGetValue("file", out var fv) ? AsStr(fv) : "";
                imagesIdx.Add((page, url, file));
            }
        }
    }


    // Chunk (your existing logic)
    var chunks = RagLib.Chunking.TokenChunker.Chunk(text, 800, 120, "cl100k_base")
        .Select((c, i) => (i, c.Text, c.FromToken, c.ToToken)).ToList();

    // Map chunk text back to character ranges in the original concatenated text
    var rows = new List<(int, string, object, float[])>(chunks.Count);
    int searchStart = 0;

    foreach (var ch in chunks)
    {
        // Find the char-span of this chunk’s text within the big text (monotonic scan)
        var idx = text.IndexOf(ch.Text, searchStart, StringComparison.Ordinal);
        if (idx < 0) idx = text.IndexOf(ch.Text.Trim(), searchStart, StringComparison.Ordinal); // mild resilience
        if (idx < 0) idx = searchStart; // last resort: fallback to cursor

        var fromChar = idx;
        var toChar = Math.Min(idx + ch.Text.Length, text.Length);
        searchStart = toChar;

        // Determine which pages overlap [fromChar, toChar)
        var pages = pageSpans
            .Where(p => !(toChar <= p.start || fromChar >= p.end))
            .Select(p => p.page)
            .Distinct()
            .OrderBy(p => p)
            .ToArray();

        // Collect image URLs for those pages
        var imgs = imagesIdx
    .Where(im => pages.Contains(im.page))
    .Select(im => im.url)
    .Distinct()
    .ToArray();

        var vec = await emb.EmbedAsync(ch.Text, default);

        var meta = new Dictionary<string, object>(baseMeta)
        {
            ["from_token"] = ch.FromToken,
            ["to_token"] = ch.ToToken,
            ["from_char"] = fromChar,
            ["to_char"] = toChar,
            ["pages"] = pages,
            ["images"] = imgs,             // <—— <<<<<< this is what you’ll consume in /query
            ["file"] = Path.GetFileName(docPath)
        };

        rows.Add((ch.i, ch.Text, meta, vec));
    }

    await store.UpsertAsync(req.DocId, rows, default);
    return Results.Ok(new { status = "ok", chunks = rows.Count });
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public record QueryRequest(string Query, int? TopK);
public record IngestRequest(string Path, string DocId);
