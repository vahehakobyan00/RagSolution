using Rag.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface ITelegramConversationService
    {
        Task<TelegramConversationDto> CreateOrGetTelegramConversationByUserIdAsync(long userId);
        Task<TelegramConversationDto> EditSessionIdByUserIdAsync(long userId);
    }
}
