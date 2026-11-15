using Rag.Data.Data;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;

namespace Rag.Data.Repositories
{
    public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
