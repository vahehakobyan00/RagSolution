namespace Rag.Data.Entity
{
    public class Turn
    {
        public long Id { get; set; }
        public long ConversationId { get; set; }
        public Conversation Conversation { get; set; } = default!;
        public int Index { get; set; } // 0..N turn number
        public string Question { get; set; } = default!;
        public string Answer { get; set; } = default!;

        // Store retrieved chunk ids only (avoid Jsonb Dictionary issue)
        public string[] RetrievedChunkIds { get; set; } = Array.Empty<string>();

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime QuestionDate { get; set; }
        public DateTime AnswerDate { get; set; }
        public TurnFeedback? Feedback { get; set; }  // optional 1:1
    }
}
