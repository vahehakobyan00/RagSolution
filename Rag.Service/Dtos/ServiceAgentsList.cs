using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ServiceAgentsList
    {
        public List<ServiceAgentGridItem> ServiceAgents { get; set; } = new List<ServiceAgentGridItem>();
        public int Count { get; set; }
    }
    public class ServiceAgentGridItem 
    {
        public long Id { get; set; }
        public string Name { get; set; }
        //public int LlmProviderId { get; set; }
        public string LlmProviderName {  get; set; }
        public string ModelName { get; set; }
        public bool IsEnabled {  get; set; }
    }

}
