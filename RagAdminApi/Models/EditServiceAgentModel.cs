namespace RagAdminApi.Models
{
    public class EditServiceAgentModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string APIToken { get; set; }
        public int LlmProviderId { get; set; }
        public string ModelName { get; set; }
        public string SystemPrompt { get; set; }
        public bool IsEnabled {  get; set; }
    }
}
