using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class GetAllConversationsStatisticsDetailsFilter
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public string UserId { get; set; }
        public long ConversationId { get; set; }
        public long? KnowledgeBaseId { get; set; }
        public long? KnowledgeBaseCategoryId { get; set; }
        public long? KnowledgeBaseDocumentId { get; set; }
        public string? TextSearchInMessages { get; set; }
    }
}
