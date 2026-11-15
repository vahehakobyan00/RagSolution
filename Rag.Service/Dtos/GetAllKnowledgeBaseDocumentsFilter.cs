using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class GetAllKnowledgeBaseDocumentsFilter
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public bool ShowAll { get; set; }
        public string UserId {  get; set; }
        public List<long>? FileItemIdsList { get; set; }
        public bool IsExplore { get; set; }
    }
}
