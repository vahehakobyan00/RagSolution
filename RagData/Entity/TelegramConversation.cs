using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Entity
{
    public class TelegramConversation
    {
        [Key]
        public long Id { get; set; }
        public long TelegramUserId {  get; set; }
        public string TelegramUserGuid { get; set; }
        public string SessionId { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
