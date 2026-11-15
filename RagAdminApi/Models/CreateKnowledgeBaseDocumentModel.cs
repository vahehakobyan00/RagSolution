namespace RagAdminApi.Models
{
    public class CreateKnowledgeBaseDocumentModel
    {
        public IFormFile? File { get; set; }
        public string? dataContent { get; set; }
    }
}
