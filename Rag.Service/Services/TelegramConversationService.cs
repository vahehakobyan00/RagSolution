using Microsoft.EntityFrameworkCore;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services
{
    public class TelegramConversationService : ITelegramConversationService
    {
        private readonly ITelegramConversationRepository _telegramConversationRepository;
        public TelegramConversationService(ITelegramConversationRepository telegramConversationRepository)
        {
            _telegramConversationRepository = telegramConversationRepository;
        }

        public async Task<TelegramConversationDto> CreateOrGetTelegramConversationByUserIdAsync(long userId)
        {
            try
            {
                TelegramConversationDto telegramConversationDto = new TelegramConversationDto() { TelegramUserId = userId };
                var tgConversation = await _telegramConversationRepository.SelectAll(x => x.TelegramUserId == userId).FirstOrDefaultAsync();
                if (tgConversation == null)
                {
                    TelegramConversation newTelegramConversation = new TelegramConversation()
                    {
                        TelegramUserId = userId,
                        CreatedUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                        SessionId = Guid.NewGuid().ToString(),
                        TelegramUserGuid = GetGuidFromTelegramId(userId).ToString()
                    };
                    _telegramConversationRepository.Insert(newTelegramConversation);
                    await _telegramConversationRepository.SaveChangesAsync();
                    telegramConversationDto.SessionId = newTelegramConversation.SessionId;
                    telegramConversationDto.TelegramUserGuid = newTelegramConversation.TelegramUserGuid;
                }
                else
                {
                    telegramConversationDto.SessionId = tgConversation.SessionId;
                    telegramConversationDto.TelegramUserGuid = tgConversation.TelegramUserGuid;
                }
                return telegramConversationDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in CreateOrGetTelegramConversationByUserIdAsync");
                throw;
            }
        }

        public async Task<TelegramConversationDto> EditSessionIdByUserIdAsync(long userId)
        {
            try
            {
                var tgConversation = await _telegramConversationRepository.SelectAll(x => x.TelegramUserId == userId).FirstOrDefaultAsync();
                if (tgConversation == null)
                {
                    var creationResult = await CreateOrGetTelegramConversationByUserIdAsync(userId);
                    return creationResult;
                }
                tgConversation.SessionId = Guid.NewGuid().ToString();
                await _telegramConversationRepository.SaveChangesAsync();
                var tgConversationDto = new TelegramConversationDto()
                {
                    SessionId = tgConversation.SessionId,
                    TelegramUserGuid = tgConversation.TelegramUserGuid,
                    TelegramUserId = userId
                };
                return tgConversationDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in EditSessionIdByUserIdAsync");
                throw;
            }
        }

        private Guid GetGuidFromTelegramId(long telegramId)
        {
            string idString = telegramId.ToString();

            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(idString));

            return new Guid(hash);
        }
    }
}
