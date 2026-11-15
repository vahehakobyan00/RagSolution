using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class GetAllKnowledgeBaseCategoriesFilter
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public string UserId { get; set; }
        public bool ShowAll { get; set; }
        public List<long>? KnowledgeBaseCategoriesIdsList { get; set; }
        public bool IsExplore { get; set; }
    }
}
