using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class KnowledgeBaseDocumentDto
    {
        public long Id { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public long FileItemId { get; set; }
        public long KnowledgeBaseId { get; set; }
        public long KnowledgeBaseCategoryId { get; set; }
        public string FilePath {  get; set; }
        public string DocId { get; set; }
        public FileItemDto? FileItem { get; set; }
        public KnowledgeBaseDto? KnowledgeBase { get; set; }
        public KnowledgeBaseCategoryDto? KnowledgeBaseCategory { get; set; }
    }
}
