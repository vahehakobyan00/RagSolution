using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagLib.Dtos
{
    public class ChunkDto
    {
        public long ChunkId { get; set; }
        public string Content { get; set; }
        public string DocId {  get; set; }
        public string? FileName {  get; set; }
    }
}
