using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using RagAdminApi.Requests;
using Serilog;

namespace RagAdminApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ChunkController : BaseController<ChunkController>
    {
        private readonly IChunkService _chunkService;
        public ChunkController(IChunkService chunkService)
        {
            _chunkService = chunkService;
        }
        [HttpPost]
        [Route("GetAllChunks")]
        public async Task<IActionResult> GetAllChunks(GetAllChunksRequest getAllChunksRequest)
        {
            try
            {
                Log.Information("GetAllChunks  for user {UserId}", UserId);
                var getAllChunksFilter = new GetAllChunksFilter()
                {
                    PageIndex = getAllChunksRequest.PageIndex,
                    PageSize = getAllChunksRequest.PageSize,
                    UserId = UserId,
                    ChunkIds = getAllChunksRequest.ChunkIds
                };
                var chunksList = await _chunkService.GetAllChunkAsync(getAllChunksFilter);
                return Ok(chunksList);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetAllChunks");
                return BadRequest(ex.Message);
            }
        }
    }
}
