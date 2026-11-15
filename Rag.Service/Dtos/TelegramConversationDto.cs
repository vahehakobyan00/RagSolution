using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class TelegramConversationDto
    {
        public long TelegramUserId { get; set; }
        public string TelegramUserGuid { get; set; }
        public string SessionId { get; set; }
    }
}
