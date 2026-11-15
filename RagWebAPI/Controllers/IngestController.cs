using Microsoft.AspNetCore.Mvc;
using RagLib.Embeddings;
using RagLib.Storage;
using RagLib.TextExtractors;
using RagWebAPI.Requests;
//using ILogger = Serilog.ILogger;

namespace RagWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IngestController : Controller
    {
        ILogger _logger;
        ITextExtractorFactory _textExtractorFactory;
        IEmbeddingClient _embeddingClient;
        VectorStore _store;
       
        public IngestController(ILogger logger,
            ITextExtractorFactory textExtractorFactory,
            IEmbeddingClient embeddingClient,
            VectorStore store)
        {
            _logger = logger;
            _textExtractorFactory = textExtractorFactory;
            _embeddingClient = embeddingClient;
            _store = store;
        }

        [HttpPost]
        [Route("fromPath")]
        public async Task<IActionResult> IngestFromPath (IngestRequest request)
        {
            try
            {
                ITextExtractor extractor = _textExtractorFactory.GetTextExtractor(request.Path);
                var docPath = request.Path;
                if (!string.IsNullOrEmpty(docPath))
                {
                    docPath = System.IO.Path.Combine("D:\\AIRAG\\RagSolution\\RagSolution\\deploy\\", docPath);
                }

                var (text, baseMeta) = await extractor.ExtractAsync(docPath, default);
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
                        ["file"] = System.IO.Path.GetFileName(docPath)
                    };
                    rows.Add((ch.i, ch.Text, meta, vec));
                }
                await _store.UpsertAsync(request.DocId, rows, default);
                return Ok(new { status = "ok", chunks = rows.Count });

            }
            catch (Exception ex)
            {
               // _logger.Error(ex, "Error on IngestFromFile");

                return BadRequest(ex.Message);
            }
        }

        //[HttpPost]
        //[Route("fromFile")]
        //public async Task<IActionResult> IngestFromFile([FromForm]IngestFileRequest request)
        //{
        //    try
        //    {
        //        ITextExtractor extractor = _textExtractorFactory.GetTextExtractor(request.File.FileName);

        //        var (text, baseMeta) = await extractor.ExtractAsync(request.File.OpenReadStream(), request.File.FileName, default);
        //        var chunks = RagLib.Chunking.TokenChunker.Chunk(text, 800, 120, "cl100k_base")
        //            .Select((c, i) => (i, c.Text, c.FromToken, c.ToToken)).ToList();

        //        var rows = new List<(int, string, object, float[])>(chunks.Count);
        //        foreach (var ch in chunks)
        //        {
        //            var vec = await _embeddingClient.EmbedAsync(ch.Text, default);
        //            var meta = new Dictionary<string, object>(baseMeta)
        //            {
        //                ["from_token"] = ch.FromToken,
        //                ["to_token"] = ch.ToToken,
        //                ["file"] = System.IO.Path.GetFileName(request.File.FileName)
        //            };
        //            rows.Add((ch.i, ch.Text, meta, vec));
        //        }
        //        await _store.UpsertAsync(request.DocId, rows, default);
        //        return Ok(new { status = "ok", chunks = rows.Count });

        //    }
        //    catch (Exception ex)
        //    {
        //       // _logger.Error(ex, "Error on IngestFromFile");

        //        return BadRequest(ex.Message);
        //    }
        //}
    }
}

