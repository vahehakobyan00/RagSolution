using System.ComponentModel.DataAnnotations;

namespace Rag.Data.Entity
{
    public class KnowledgeBase
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long ServiceAgentId {  get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public KnowledgeBase ToUpdated(KnowledgeBase dbEntity)
        {
            dbEntity.Name = this.Name;
            dbEntity.Description = this.Description;
            dbEntity.ServiceAgentId = this.ServiceAgentId;
            dbEntity.UpdatedByUserId = this.UpdatedByUserId;
            dbEntity.LastModifiedDate = this.LastModifiedDate;
            return dbEntity;
        }
    }
}
