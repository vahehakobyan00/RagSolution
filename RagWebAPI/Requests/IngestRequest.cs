using System.ComponentModel.DataAnnotations;

namespace RagWebAPI.Requests
{
    public class IngestFileRequest
    {
        [Required]
        public string DocId { get; set; } 

        [Required]
        public IFormFile File { get; set; }
    }

    public class IngestRequest
    {
        [Required]
        public string DocId { get; set; }

        [Required]
        public string Path { get; set; }
    }
    
}
