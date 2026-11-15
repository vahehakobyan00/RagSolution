namespace Rag.Service.Dtos
{
    public class TurnFeedbackViewDto
    {
        public long Id { get; set; }
        public long TurnId { get; set; }
        public int Value { get; set; }
        public DateTime SetUtc { get; set; } = DateTime.UtcNow;

    }
}
