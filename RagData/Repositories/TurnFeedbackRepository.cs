using Rag.Data.Data;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;

namespace Rag.Data.Repositories
{
    public class TurnFeedbackRepository : BaseRepository<TurnFeedback>, ITurnFeedbackRepository
    {
        public TurnFeedbackRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
