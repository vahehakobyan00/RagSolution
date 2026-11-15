namespace RagAdminApi.Models
{
    public class EditKnowledgeBaseModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long ServiceAgentId { get; set; }
    }
}
