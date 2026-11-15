using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class CreateEditServiceAgentDto
    {
        public ServiceAgentDto ServiceAgent { get; set; }
        public bool IsEdit {  get; set; }
    }
}
