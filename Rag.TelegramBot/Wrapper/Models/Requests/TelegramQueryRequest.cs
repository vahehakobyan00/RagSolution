using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.TelegramBot.Wrapper.Models.Requests
{
    public class TelegramQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string UserId {  get; set; } = string.Empty;
    }
}
