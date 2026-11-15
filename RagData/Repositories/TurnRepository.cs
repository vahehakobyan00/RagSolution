using Rag.Data.Data;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;

namespace Rag.Data.Repositories
{
    public class TurnRepository : BaseRepository<Turn>, ITurnRepository
    {
        public TurnRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
