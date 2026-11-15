namespace Rag.Service.Dtos
{
    public class ConversationViewDto
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public List<TurnViewDto> Turns { get; set; } = new List<TurnViewDto>();
    }
}
