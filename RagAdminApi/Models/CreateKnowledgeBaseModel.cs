namespace RagAdminApi.Models
{
    public class CreateKnowledgeBaseModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public long ServiceAgentId { get; set; }
    }
}
