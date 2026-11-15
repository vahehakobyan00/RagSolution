using Rag.Data.Data;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Repositories
{
    public class TelegramConversationRepository : BaseRepository<TelegramConversation>, ITelegramConversationRepository
    {
        public TelegramConversationRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
