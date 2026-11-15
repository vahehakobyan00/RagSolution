namespace RagAdminApi.Requests
{
    public class GetAllKnowledgeBaseCategoriesRequest
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public bool ShowAll { get; set; }
        public List<long>? KnowledgeBaseCategoriesIdsList {  get; set; }
        public bool IsExplore {  get; set; }
    }
}
