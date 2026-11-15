using Rag.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface IConversationStatisticsService
    {
        Task<ConversationsStatisticsList> GetAllConversationsStatisticsAsync(GetAllConversationsStatisticsFilter getAllConversationsFilter);
        Task<ConversationStatisticsDetailsList> GetConversationDetailsByIdAsync(GetAllConversationsStatisticsDetailsFilter getAllConversationsStatisticsDetailsFilter);
    }
}
