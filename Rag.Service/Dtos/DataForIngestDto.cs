using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class DataForIngestDto
    {
        public string? DocId {  get; set; }
        public string FilePath {  get; set; }
        public string FileName { get; set; }
    }
}
