using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class IngestResultDto
    {
        public string Status { get; set; }
        public long ChunksCount { get; set; }
        public DateTime ExcecutionDate { get; set; }
    }
}
