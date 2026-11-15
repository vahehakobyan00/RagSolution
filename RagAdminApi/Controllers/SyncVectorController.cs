using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rag.Service.Services.Interfaces;
using Serilog;

namespace RagAdminApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SyncVectorController : Controller
    {
        private readonly ISyncVectoreService _syncVectoreService;
        public SyncVectorController(ISyncVectoreService syncVectoreService)
        {
            _syncVectoreService = syncVectoreService;
        }
        [HttpGet]
        [Route("SyncVector")]
        public async Task<IActionResult> SyncVector(long kbDocumentId)
        {
            try
            {
                var res = await _syncVectoreService.SyncVectore(kbDocumentId);
                if (!res.HasError)
                {
                    return Ok(res.Message);                
                }
                return BadRequest(res.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in SyncVector");
                return BadRequest(ex.Message);
            }
        }
    }
}
