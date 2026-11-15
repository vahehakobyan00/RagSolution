using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ServiceAgentConfigDto
    {
        public string APIToken { get; set; }
        public string ModelName { get; set; }
        public string SystemPrompt { get; set; }
        public int LlmProviderId {  get; set; }
    }
}
