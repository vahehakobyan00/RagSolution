using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Entity
{
    public class KnowledgeBaseCategory
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsDeleted { get; set; }

        public KnowledgeBaseCategory ToUpdated(KnowledgeBaseCategory dbEntity)
        {
            dbEntity.Name = this.Name;
            dbEntity.Description = this.Description;
            dbEntity.UpdatedByUserId = this.UpdatedByUserId;
            dbEntity.LastModifiedDate = this.LastModifiedDate;

            return dbEntity;
        }
    }
}
