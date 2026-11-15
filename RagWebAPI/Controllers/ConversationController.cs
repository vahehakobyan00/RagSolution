using Microsoft.AspNetCore.Mvc;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using RagWebAPI.Middleware;
using Serilog;

namespace RagWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConversationController : ControllerBase
    {
        IHistoryService _historyService;

        public ConversationController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        [HttpGet]
        [Route("GetById")]
        public async Task<IActionResult> GetConversationById(long id)
        {
            try
            {
                var uid = HttpContext.GetUserId();
                var conversation = await _historyService.GetConversationByIdAsync(id, uid!);
                return Ok(conversation);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetConversationById");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var uid = HttpContext.GetUserId();
                var conversations = await _historyService.GetConversationsAsync(uid!);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetAll conversations");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("SendFeedback")]
        public async Task<IActionResult> SendFeedback(SendFeedBackRequest request)
        {
            try
            {
                await _historyService.SendFeedBackAsync(request.Id, request.Value);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetConversationById");
                return BadRequest(ex.Message);
            }
        }
    }
}
