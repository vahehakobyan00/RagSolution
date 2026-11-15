using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class StatisticsFilter
    {
        public long? KnowledgeBaseId { get; set; }
        public long? KnowledgeBaseCategoryId { get; set; }
        public long? KnowledgeBaseDocumentId { get; set; }
        public int? MinInteractionsCount { get; set; }
        public string? TextSearchInMessages { get; set; }
    }
}
