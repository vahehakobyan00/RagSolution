using OpenAI;
using OpenAI.Chat;
using Rag.Service.Factory.Interfaces;
using System.Runtime.CompilerServices;

namespace Rag.Service.Factory
{
    public class OpenAiLlmProvider : ILlmProvider
    {
        private readonly OpenAIClient _client;
        private readonly string _model;

        public OpenAiLlmProvider(string apiKey, string model)
        {
            _client = new OpenAIClient(apiKey: apiKey);
            _model = model;
        }

        public async IAsyncEnumerable<string> StreamAsync(IEnumerable<ChatMessage> messages, [EnumeratorCancellation] CancellationToken ct)
        {
            var chatClient = _client.GetChatClient(_model);
            var options = new OpenAI.Chat.ChatCompletionOptions { Temperature = 0.2f };

            await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, options, ct))
            {
                if (update.ContentUpdate is { Count: > 0 })
                {
                    foreach (var part in update.ContentUpdate)
                    {
                        if (part.Kind == OpenAI.Chat.ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
                        {
                            yield return part.Text;
                        }
                    }
                }
            }
        }

        public async Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct)
        {
            var chatClient = _client.GetChatClient(_model);
            var result = await chatClient.CompleteChatAsync(messages, cancellationToken: ct);
            return result.Value.Content[0].Text;
        }
    }
}
