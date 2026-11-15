using Microsoft.AspNetCore.Http;

namespace Rag.Service.FileServices.Requests
{
    public class FileUploadRequest
    {
        public IFormFile? file { get; set; }
        public string CreatorId { get; set; }
        public string? Description { get; set; }
    }
}
