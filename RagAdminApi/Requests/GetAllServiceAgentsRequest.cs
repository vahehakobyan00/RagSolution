namespace RagAdminApi.Requests
{
    public class GetAllServiceAgentsRequest
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public bool ShowAll { get; set; }
    }
}
