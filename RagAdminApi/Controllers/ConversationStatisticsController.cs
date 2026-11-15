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
    public class ConversationStatisticsController : BaseController<ConversationStatisticsController>
    {
        private readonly IConversationStatisticsService _conversationStatisticsService;
        public ConversationStatisticsController(IConversationStatisticsService conversationStatisticsService)
        {
            _conversationStatisticsService = conversationStatisticsService;
        }
        [HttpPost]
        [Route("GetAllConversationStatistics")]
        public async Task<IActionResult> GetAllConversationStatistics(GetAllConversationsStatisticsRequest getAllConversationsStatisticsRequest)
        {
            try
            {
                Log.Information("GetAllConversationStatistics  for user {UserId}", UserId);
                var getAllConversationsStatisticsFilter = new GetAllConversationsStatisticsFilter()
                {
                    PageIndex = getAllConversationsStatisticsRequest.PageIndex,
                    PageSize = getAllConversationsStatisticsRequest.PageSize,
                    UserId = UserId,
                    ShowAll = getAllConversationsStatisticsRequest.ShowAll,
                    KnowledgeBaseCategoryId = getAllConversationsStatisticsRequest?.KnowledgeBaseCategoryId,
                    KnowledgeBaseDocumentId = getAllConversationsStatisticsRequest?.KnowledgeBaseDocumentId,
                    KnowledgeBaseId = getAllConversationsStatisticsRequest?.KnowledgeBaseId,
                    MinInteractionsCount = getAllConversationsStatisticsRequest?.MinInteractionsCount
                };
                var conversationStatisticsList = await _conversationStatisticsService.GetAllConversationsStatisticsAsync(getAllConversationsStatisticsFilter);
                return Ok(conversationStatisticsList);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetAllConversationStatistics");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetConversationStatisticsDetails")]
        public async Task<IActionResult> GetConversationStatisticsDetails(GetAllConversationsStatisticsDetailsRequest getAllConversationsStatisticsDetailsRequest)
        {
            try
            {
                Log.Information("GetConversationStatisticsDetails  for user {UserId}", UserId);
                var getAllConversationsStatisticsFilter = new GetAllConversationsStatisticsDetailsFilter()
                {
                    PageIndex = getAllConversationsStatisticsDetailsRequest.PageIndex,
                    PageSize = getAllConversationsStatisticsDetailsRequest.PageSize,
                    UserId = UserId,
                    ConversationId = getAllConversationsStatisticsDetailsRequest.ConversationId,
                    KnowledgeBaseCategoryId = getAllConversationsStatisticsDetailsRequest?.KnowledgeBaseCategoryId,
                    KnowledgeBaseDocumentId = getAllConversationsStatisticsDetailsRequest?.KnowledgeBaseDocumentId,
                    KnowledgeBaseId = getAllConversationsStatisticsDetailsRequest?.KnowledgeBaseId,
                    TextSearchInMessages = getAllConversationsStatisticsDetailsRequest?.TextSearchInMessages
                };
                var conversationDetails = await _conversationStatisticsService.GetConversationDetailsByIdAsync(getAllConversationsStatisticsFilter);
                return Ok(conversationDetails);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetConversationStatisticsDetails");
                return BadRequest(ex.Message);
            }
        }
    }
}
