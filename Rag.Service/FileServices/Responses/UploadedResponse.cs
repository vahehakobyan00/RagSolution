using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.FileServices.Responses
{
    public class UploadedResponse
    {
        public bool Uploaded { get; set; }
        public string? UploadedFilename { get; set; }
        public string? UploadedFilePath { get; set; }
        public long FileSize { get; set; }
    }
}
