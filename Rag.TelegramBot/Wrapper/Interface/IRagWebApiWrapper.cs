using Rag.TelegramBot.Wrapper.Models.Requests;
using Rag.TelegramBot.Wrapper.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.TelegramBot.Wrapper.Interface
{
    public interface IRagWebApiWrapper
    {
        Task<TelegramPromptResponse> GetAnswerAsync(TelegramQueryRequest queryRequest);
    }
}
