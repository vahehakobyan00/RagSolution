using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ServiceAgentDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string APIToken { get; set; }
        public int LlmProviderId { get; set; }
        public string ModelName { get; set; }
        public string SystemPrompt { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEnabled {  get; set; }
    }
}
