using Rag.Data.Data;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;

namespace Rag.Data.Repositories
{
    public class MenuItemRepository : BaseRepository<MenuItem>, IMenuItemRepository
    {
        public MenuItemRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
