using Microsoft.AspNetCore.Http;
using Rag.Service.Dtos;
using Rag.Service.FileServices.Interfaces;
using RagLib.Embeddings;
using RagLib.Storage;
using RagLib.TextExtractors;
using Serilog;

namespace Rag.Service.FileServices.Services
{
    public class IngestService : IIngestService
    {
        private readonly ITextExtractorFactory _textExtractorFactory;
        private readonly IEmbeddingClient _embeddingClient;
        private readonly IFileService _fileService;
        private readonly IUploaderService _uploaderService;

        VectorStore _store;
        public IngestService(ITextExtractorFactory textExtractorFactory, 
            IEmbeddingClient embeddingClient, 
            VectorStore vectorStore, 
            IFileService fileService,
            IUploaderService uploaderService)
        {
            _textExtractorFactory = textExtractorFactory;
            _embeddingClient = embeddingClient;
            _store = vectorStore;
            _fileService = fileService;
            _uploaderService = uploaderService;
        }
        public async Task<IngestResultDto> IngestFromFileAsync(IFormFile File, string DocId)
        {
            try
            {
                var extension = System.IO.Path.GetExtension(File.FileName);
                ITextExtractor extractor = _textExtractorFactory.GetTextExtractor(extension);

                var (text, baseMeta) = await extractor.ExtractAsync(File.OpenReadStream(), File.FileName, default);
                var chunks = RagLib.Chunking.TokenChunker.Chunk(text, 800, 120, "cl100k_base")
                    .Select((c, i) => (i, c.Text, c.FromToken, c.ToToken)).ToList();

                var rows = new List<(int, string, object, float[])>(chunks.Count);
                foreach (var ch in chunks)
                {
                    var vec = await _embeddingClient.EmbedAsync(ch.Text, default);
                    var meta = new Dictionary<string, object>(baseMeta)
                    {
                        ["from_token"] = ch.FromToken,
                        ["to_token"] = ch.ToToken,
                        ["file"] = System.IO.Path.GetFileName(File.FileName)
                    };
                    rows.Add((ch.i, ch.Text, meta, vec));
                }
               
                await _store.UpsertAsync(DocId, rows, default);
                return new IngestResultDto()
                {
                    ChunksCount = rows.Count,
                    Status = "ok",
                    ExcecutionDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on IngestFromFile");
                throw;
            }
        }

        public async Task<IngestResultDto> IngestFromPathAsync(string filePath, string DocId, string extension)
        {
            try
            {
                //filePath = System.IO.Path.Combine("D:\\", "file.pdf");
                //extension = ".pdf";
                ITextExtractor extractor = _textExtractorFactory.GetTextExtractor(extension);
                var docPath = filePath;
                var (text, baseMeta) = await extractor.ExtractAsync(docPath, default);
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
                var imagesIdx = new List<(int page, string bytes, string file)>();
                if (baseMeta.TryGetValue("images", out var imObj) && imObj is IEnumerable<object> imList)
                {
                    foreach (var o in imList)
                    {
                        if (o is IDictionary<string, object> m)
                        {
                            int AsInt(object v) => v is null ? 0 : Convert.ToInt32(v);
                            string AsStr(object v) => v?.ToString() ?? "";
                            //byte[] AsBytes(object v) => v is null ? null : Convert.ToByte(v);

                            var page = m.TryGetValue("page", out var pv) ? AsInt(pv) : 0;
                            var bytes = m.TryGetValue("bytes", out var uv) ? AsStr(uv) : "";
                            var file = m.TryGetValue("file", out var fv) ? AsStr(fv) : "";
                            imagesIdx.Add((page, bytes, file));
                        }
                    }
                }
                // Chunk (your existing logic)
                var chunks = RagLib.Chunking.TokenChunker.Chunk(text, 800, 120, "cl100k_base")
                    .Select((c, i) => (i, c.Text, c.FromToken, c.ToToken)).ToList();

                // Map chunk text back to character ranges in the original concatenated text
                var rows = new List<(int, string, object, float[])>(chunks.Count);
                var rowsForImagesSave = new List<(int, string, object, float[])>(chunks.Count);
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
                .Select(im => im.bytes)
                .Distinct()
                .ToArray();

                   
                    var imgsUrls = new List<string>();
                    foreach (var img in imgs)
                    {
                        string fileNameWithoutExt = Guid.NewGuid().ToString().Replace("-", "");
                        var fullPath = _uploaderService.GetFullPath(img, fileNameWithoutExt);
                        imgsUrls.Add(Path.Combine(fullPath));
                    }
                    var vec = await _embeddingClient.EmbedAsync(ch.Text, default);

                    var meta = new Dictionary<string, object>(baseMeta)
                    {
                        ["from_token"] = ch.FromToken,
                        ["to_token"] = ch.ToToken,
                        ["from_char"] = fromChar,
                        ["to_char"] = toChar,
                        ["pages"] = pages,
                        ["images"] = imgs,             // <—— <<<<<< this is what you’ll consume in /query
                        ["file"] = System.IO.Path.GetFileName(docPath)
                    };
                    var metaForChunk = new Dictionary<string, object>(baseMeta)
                    {
                        ["from_token"] = ch.FromToken,
                        ["to_token"] = ch.ToToken,
                        ["images"] = imgsUrls.ToArray(),             // <—— <<<<<< this is what you’ll consume in /query
                        ["file"] = System.IO.Path.GetFileName(docPath)
                    };

                    rows.Add((ch.i, ch.Text, metaForChunk, vec));
                    rowsForImagesSave.Add((ch.i, ch.Text, meta, vec));
                }

                var ids =  await _store.UpsertAsync(DocId, rows, default);
                for (var i = 0; i < ids.Count; i++)
                {
                    var images = ((Dictionary<string, object>)rowsForImagesSave[i].Item3);
                    var imgs = (string[])(images["images"]);
                    var imagesUrl = ((Dictionary<string, object>)rows[i].Item3);
                    var imgsUrl = (string[])(imagesUrl["images"]);
                    if (imgs.Length > 0)
                    {
                        for (var j = 0; j < imgs.Count(); j++)
                        {
                            var info = await _fileService.UploadExtractedImageAsync(new Requests.ExtractedImageUploadRequest { 
                            Image = imgs[j],
                             ChunkId = ids[i],
                            FullPath = imgsUrl[j]
                            });
                        }
                    }
                }
                return new IngestResultDto()
                {
                    ChunksCount = rows.Count,
                    Status = "ok",
                    ExcecutionDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on IngestFromFile");
                throw;
            }
        }
    }
}
