using Rag.Data.Entity;
using Rag.Service.Dtos;

namespace Rag.Service.Services.Interfaces
{
    public interface IHistoryService
    {
        Task<Conversation> GetOrCreateAsync(string userId, string sessionId);
        Task<int> GetNextTurnIndexAsync(long conversationId);
        Task<TurnViewDto> SaveTurnAsync(long conversationId, int index, string question, string answer, IEnumerable<string> chunkIds, DateTime questionDateTime, DateTime answerDateTime);
        Task UpdateTitleAsync(long conversationId, string title);
        Task<List<Turn>> GetRecentTurnsAsync(long conversationId, int take);
        Task<List<ConversationViewDto>> GetConversationsAsync(string userId);
        Task<ConversationViewDto> GetConversationByIdAsync(long conversationId, string userId);
        Task SendFeedBackAsync(long turnId, int value);
    }
}
