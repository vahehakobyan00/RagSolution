using Rag.Data.Data;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;

namespace Rag.Data.Repositories
{
    public class FileItemRepository : BaseRepository<FileItem>, IFileItemRepository
    {
        public FileItemRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
