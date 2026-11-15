using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ConversationStatisticsTurnDto
    {
        public DateTime QuestionDate { get; set; }
        public string Question { get; set; }
        public DateTime? AnswerDate { get; set; }
        public string Answer { get; set; }
        public int? FeedbackValue { get; set; }
        public string[] ChunkIds { get; set; }
    }
}
