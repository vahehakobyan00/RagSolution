using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ConversationsStatisticsList
    {
        public List<ConversationStatisticsGridItem> Conversations { get; set; } = new List<ConversationStatisticsGridItem>();
        public int Count { get; set; }
    }
    public class ConversationStatisticsGridItem
    {
        public long Id { get; set; }
        public string Title { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public DateTime CreationDate { get; set; }
        public int InteractionsCount { get; set; }
        public string KnowledgeBasesUsed { get; set; } = "0";
        public string KnowledgeBaseCategoriesUsed { get; set; } = "0";
        public string DocumentsUsed { get; set; } = "0";
        public int ChunksRetrievedCount { get; set; }

        // 🔹 Internal navigation data (not shown in UI)
        public List<long> KnowledgeBaseIds { get; set; } = new();
        public List<long> KnowledgeBaseCategoryIds { get; set; } = new();
        public List<long> DocumentIds { get; set; } = new();
        public List<long> ChunkIds { get; set; } = new();
    }
}
