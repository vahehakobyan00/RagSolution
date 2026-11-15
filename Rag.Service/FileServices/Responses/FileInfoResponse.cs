using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.FileServices.Responses
{
    public class FileInfoResponse
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public long FileSize { get; set; }
        public string? FilePath { get; set; }
        public string? DownloadPath { get; set; }
    }
}
