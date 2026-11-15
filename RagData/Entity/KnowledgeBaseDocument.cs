using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Entity
{
    public class KnowledgeBaseDocument
    {
        [Key]
        public long Id { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public string? DocId {  get; set; }
        public DateTime? VectorStoreSyncDate { get; set; }
        public long FileItemId {  get; set; }
        public FileItem FileItem { get; set; }
        public long KnowledgeBaseId { get; set; }
        public KnowledgeBase KnowledgeBase { get; set; } = null!;
        public long KnowledgeBaseCategoryId { get; set; }
        public KnowledgeBaseCategory KnowledgeBaseCategory { get; set; } = null!;

        public KnowledgeBaseDocument ToUpdated(KnowledgeBaseDocument dbEntity)
        {
            dbEntity.FileItemId = this.FileItemId;
            dbEntity.KnowledgeBaseId = this.KnowledgeBaseId;
            dbEntity.KnowledgeBaseCategoryId = this.KnowledgeBaseCategoryId;
            dbEntity.UpdatedByUserId = this.UpdatedByUserId;
            dbEntity.LastModifiedDate = this.LastModifiedDate;
            return dbEntity;
        }
    }

}
