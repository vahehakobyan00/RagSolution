using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;

namespace Rag.Service.Services
{
    public class HistoryService : IHistoryService
    {
        IConversationRepository _conversationRepository;
        ITurnRepository _turnRepository;
        ITurnFeedbackRepository _turnFeedbackRepository;
        IMapper _mapper;
        public HistoryService(
            IConversationRepository conversationRepository,
            ITurnRepository turnRepository,
            ITurnFeedbackRepository turnFeedbackRepository,
            IMapper mapper)
        {
            _conversationRepository = conversationRepository;
            _turnRepository = turnRepository;
            _turnFeedbackRepository = turnFeedbackRepository;
            _mapper = mapper;
        }

        public async Task<ConversationViewDto> GetConversationByIdAsync(long conversationId, string userId)
        {
            var conversation = await _conversationRepository.SelectAll(x => x.Id == conversationId && x.UserId == userId).Include(x => x.Turns.OrderBy(x=>x.Index)).ThenInclude(x => x.Feedback).FirstOrDefaultAsync();
            return _mapper.Map<ConversationViewDto>(conversation);
        }

        public async Task<List<ConversationViewDto>> GetConversationsAsync(string userId)
        {
            var conversations = await _conversationRepository.SelectAll(x => x.UserId == userId).OrderByDescending(x=>x.UpdatedUtc).Take(20).ToListAsync();
            return _mapper.Map<List<ConversationViewDto>>(conversations);
        }

        public async Task<int> GetNextTurnIndexAsync(long conversationId)
        {
            var query = _turnRepository.SelectAll(x => x.ConversationId == conversationId);

            // Check if any record exists first
            var hasTurns = await query.AnyAsync();
            if (!hasTurns)
                return 1; // start at 1 if no turns

            var maxIndex = await query.MaxAsync(x => x.Index);
            return maxIndex + 1;
        }

        public async Task<Data.Entity.Conversation> GetOrCreateAsync(string userId, string sessionId)
        {
            var conversation = await _conversationRepository.SelectAll(x=>x.UserId == userId && x.SessionId == sessionId).FirstOrDefaultAsync();
            if (conversation == null)
            {
                var newConversation = new Data.Entity.Conversation
                {
                    UserId = userId,
                    SessionId = sessionId,
                    Title = string.Empty,
                    CreatedUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                    UpdatedUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };
                await _conversationRepository.InsertAsync(newConversation);
                await _conversationRepository.SaveChangesAsync();

                return newConversation;
            }
            return conversation;
        }

        public Task<List<Turn>> GetRecentTurnsAsync(long conversationId, int take)
        {
            var turns = _turnRepository.SelectAll(x=>x.ConversationId == conversationId).OrderByDescending(x=>x.Id).Take(take).AsNoTracking().ToListAsync();
            return turns;
        }

        public async Task<TurnViewDto> SaveTurnAsync(long conversationId, int index, string question, string answer, IEnumerable<string> chunkIds, DateTime questionDateTime, DateTime answerDateTime)
        {
            var turn = new Turn()
            {
                ConversationId = conversationId,
                Index = index,
                Question = question,
                Answer = answer,
                RetrievedChunkIds = chunkIds?.ToArray() ?? Array.Empty<string>(),
                CreatedUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                QuestionDate = questionDateTime,
                AnswerDate = answerDateTime             
            };

            await _turnRepository.InsertAsync(turn);
            await _turnRepository.SaveChangesAsync();
            return _mapper.Map<TurnViewDto>(turn);
        }

        public async Task SendFeedBackAsync(long turnId, int value)
        {
            var turn = await _turnRepository.SelectAll(x=>x.Id == turnId).Include(x=>x.Feedback).FirstOrDefaultAsync();
            if (turn == null)
                throw new ArgumentNullException();
            if(turn.Feedback is not null)
            {
                turn.Feedback.Value = value;
                turn.Feedback.SetUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

                _turnFeedbackRepository.Update(turn.Feedback);
                await _turnFeedbackRepository.SaveChangesAsync();
            }
            else
            {
                var feedback = new TurnFeedback()
                {
                    TurnId = turnId,
                    Value = value,
                    SetUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };
                await _turnFeedbackRepository.InsertAsync(feedback);
                await _turnFeedbackRepository.SaveChangesAsync();
            }
        }

        public async Task UpdateTitleAsync(long conversationId, string title)
        {
            var conversation = await _conversationRepository.SelectAll(x => x.Id == conversationId).FirstOrDefaultAsync();
            if(conversation is not null)
            {
                conversation.Title = title;
                conversation.UpdatedUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                _conversationRepository.Update(conversation);
                await _conversationRepository.SaveChangesAsync();
            }
        }
    }
}
