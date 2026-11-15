using System.ComponentModel.DataAnnotations;

namespace Rag.Data.Entity
{
    public class Conversation
    {
        [Key]
        public long Id { get; set; }
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; } 
        public List<Turn> Turns { get; set; } = new List<Turn>();
    }
}
