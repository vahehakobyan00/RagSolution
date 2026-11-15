namespace RagAdminApi.Requests
{
    public class GetAllChunksRequest
    {
        public int PageSize { get; set; } = int.MaxValue;
        public int PageIndex { get; set; } = 0;
        public List<long>? ChunkIds { get; set; }
    }
}
