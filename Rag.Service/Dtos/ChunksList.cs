using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class ChunksList
    {
        public List<ChunkGridItem> Chunks { get; set; } = new List<ChunkGridItem>();
        public int Count { get; set; }
    }
    public class ChunkGridItem
    {
        public long ChunkId {  get; set; }
        public string Content {  get; set; }
        public string? FileName {  get; set; }
    }
}
