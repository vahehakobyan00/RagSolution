using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ConversationStatisticsDetailsList
    {      
        public List<ConversationStatisticsDetailsGridItem> ConversationStatisticsDetails { get; set; } = new List<ConversationStatisticsDetailsGridItem>();
        public int Count { get; set; }
    }
    public class ConversationStatisticsDetailsGridItem
    {
        public string Title { get; set; }
        public DateTime MessageDate {  get; set; }
        public InteractionsRoleModel interactionsRoleModel { get; set; }
        public string Message {  get; set; }
        public int? FeedbackValue {  get; set; }
        public string KBUsed { get; set; } = "0";
        public string KBCategoriesUsed { get; set; } = "0";
        public string DocumentsUsed { get; set; } = "0";
        public int ChunksRetrievedCount { get; set; }

        // 🔹 Internal navigation data (not shown in UI)
        public List<long> KnowledgeBaseIds { get; set; } = new();
        public List<long> KnowledgeBaseCategoryIds { get; set; } = new();
        public List<long> DocumentIds { get; set; } = new();
        public List<long> ChunkIds { get; set; } = new();
    }

}
