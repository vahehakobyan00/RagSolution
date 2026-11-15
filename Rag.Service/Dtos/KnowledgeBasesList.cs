using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class KnowledgeBasesList
    {
        public List<KnowledgeBaseGridItem> KnowledgeBases { get; set; } = new List<KnowledgeBaseGridItem>();
        public int Count { get; set; }
    }
    public class KnowledgeBaseGridItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ServiceAgentName { get; set; }
    }
}
