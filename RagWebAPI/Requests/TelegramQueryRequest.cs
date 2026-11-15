namespace RagWebAPI.Requests
{
    public class TelegramQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
