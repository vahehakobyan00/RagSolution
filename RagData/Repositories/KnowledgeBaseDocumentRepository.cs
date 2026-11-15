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
    public class KnowledgeBaseDocumentRepository : BaseRepository<KnowledgeBaseDocument>, IKnowledgeBaseDocumentRepository
    {
        public KnowledgeBaseDocumentRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
