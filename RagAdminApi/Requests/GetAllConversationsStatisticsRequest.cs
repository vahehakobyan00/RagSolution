namespace RagAdminApi.Requests
{
    public class GetAllConversationsStatisticsRequest
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public bool ShowAll { get; set; }
        public long? KnowledgeBaseId {  get; set; }
        public long? KnowledgeBaseCategoryId { get; set; }
        public long? KnowledgeBaseDocumentId { get; set; }
        public int? MinInteractionsCount {  get; set; }
    }
}
