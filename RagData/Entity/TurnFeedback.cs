namespace Rag.Data.Entity
{
    public class TurnFeedback
    {
        public long Id { get; set; }
        public long TurnId { get; set; }
        public Turn Turn { get; set; } = default!;

        // 0=Neutral, 1=Like, -1=Dislike
        public int Value { get; set; }
        public DateTime SetUtc { get; set; } = DateTime.UtcNow;
    }
}
