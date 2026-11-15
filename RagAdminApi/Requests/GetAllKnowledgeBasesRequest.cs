namespace RagAdminApi.Requests
{
    public class GetAllKnowledgeBasesRequest
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public bool ShowAll { get; set; }
        public List<long>? KnowledgeBasesIdsList { get; set; }
        public bool IsExplore { get; set; }
    }
}
