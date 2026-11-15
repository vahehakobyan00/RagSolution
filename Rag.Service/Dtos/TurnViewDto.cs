using Rag.Data.Entity;

namespace Rag.Service.Dtos
{
    public class TurnViewDto
    {
        public long Id { get; set; }
        public long ConversationId { get; set; }
        public int Index { get; set; } // 0..N turn number
        public string Question { get; set; } = default!;
        public string Answer { get; set; } = default!;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public TurnFeedbackViewDto? Feedback { get; set; }
    }
}
