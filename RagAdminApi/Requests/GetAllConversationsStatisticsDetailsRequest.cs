namespace RagAdminApi.Requests
{
    public class GetAllConversationsStatisticsDetailsRequest
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public long ConversationId {  get; set; }
        public long? KnowledgeBaseId { get; set; }
        public long? KnowledgeBaseCategoryId { get; set; }
        public long? KnowledgeBaseDocumentId { get; set; }
        public string? TextSearchInMessages { get; set; }
    }
}
