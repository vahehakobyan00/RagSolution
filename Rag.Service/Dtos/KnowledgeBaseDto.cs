using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class KnowledgeBaseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long ServiceAgentId { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public string UpdatedByUserId { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
