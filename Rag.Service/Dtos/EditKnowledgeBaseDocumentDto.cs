using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class EditKnowledgeBaseDocumentDto
    {
        public long Id { get; set; }
        public string EditedByUserId { get; set; }
        public long FileItemId { get; set; }
        public long KnowledgeBaseId { get; set; }
        public long KnowledgeBaseCategoryId { get; set; }
        public string? Description { get; set; }
    }
}
