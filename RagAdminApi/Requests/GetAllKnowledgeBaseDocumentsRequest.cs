namespace RagAdminApi.Requests
{
    public class GetAllKnowledgeBaseDocumentsRequest
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public bool ShowAll { get; set; }
        public List<long>? FileItemIdsList { get; set; }
        public bool IsExplore { get; set; }
    }
}
