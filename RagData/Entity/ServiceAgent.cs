using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Entity
{
    public class ServiceAgent
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string APIToken {  get; set; }
        public int LlmProviderId {  get; set; }
        [ForeignKey(nameof(LlmProviderId))]
        public LlmProvider LlmProvider { get; set; }
        public string ModelName {  get; set; }
        public string SystemPrompt {  get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsDeleted {  get; set; }
        public bool IsEnabled {  get; set; }

        public ServiceAgent ToUpdated(ServiceAgent dbEntity)
        {
            dbEntity.Name = this.Name;
            dbEntity.Description = this.Description;
            dbEntity.APIToken = this.APIToken;
            dbEntity.LlmProviderId = this.LlmProviderId;
            dbEntity.ModelName = this.ModelName;
            dbEntity.SystemPrompt = this.SystemPrompt;
            dbEntity.UpdatedByUserId = this.UpdatedByUserId;
            dbEntity.LastModifiedDate = this.LastModifiedDate;
            dbEntity.IsEnabled = this.IsEnabled;

            return dbEntity;
        }
    }
}
