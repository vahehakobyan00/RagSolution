using OpenAI;

namespace RagWebAPI.Context
{
    public interface ITitleGenerator
    {
        Task<string> GenerateAsync(IEnumerable<OpenAI.Chat.ChatMessage> messages, CancellationToken ct);
    }

    public class TitleGenerator : ITitleGenerator
    {
        private readonly OpenAIClient _client;
        private readonly string _model;

        public TitleGenerator(OpenAIClient client, string model)
        {
            _client = client;
            _model = model;
        }

        public async Task<string> GenerateAsync(IEnumerable<OpenAI.Chat.ChatMessage> messages, CancellationToken ct)
        {
            var chat = _client.GetChatClient(_model);
            var prompt = OpenAI.Chat.ChatMessage.CreateSystemMessage(
                "Generate a concise 5–8 word title for this conversation so far. No punctuation at end.");
            var last10 = messages.TakeLast(10).ToList();

            var res = await chat.CompleteChatAsync([prompt, .. last10], new OpenAI.Chat.ChatCompletionOptions { Temperature = 0.2f }, ct);
            var title = res.Value.Content[0].Text?.Trim();
            return string.IsNullOrWhiteSpace(title) ? "New conversation" : title;
        }
    }

}
