using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ConversationStatisticsDetailsDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = default!;
        public int TurnsCount { get; set; }
        public List<ConversationStatisticsTurnDto> Turns { get; set; }
    }
}
