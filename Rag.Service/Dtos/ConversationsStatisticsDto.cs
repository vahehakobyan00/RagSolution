using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ConversationsStatisticsDto
    {
        public long Id { get; set; }
        public string Title { get; set; } 
        public string UserId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int InteractionsCount { get; set; }
        public List<string> ChunkIds { get; set; } 
    }
}
