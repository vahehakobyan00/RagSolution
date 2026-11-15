namespace RagWebAPI.Requests
{
    public class QueryRequest
    {
        //public int? TopK { get; set; }
        public string Query { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty ;
        //public string UserId { get; set; } = string.Empty;
    }
}
