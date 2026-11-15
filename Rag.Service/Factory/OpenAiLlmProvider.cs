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

        public async Task<ToolRunResult> CompleteWithToolsAsync(
            List<ChatMessage> messages,
            IReadOnlyList<ChatTool> tools,
            Func<string, string, CancellationToken, Task<string>> executeTool,
            CancellationToken ct,
            int maxIterations = 6)
        {
            var chatClient = _client.GetChatClient(_model);
            var options = new ChatCompletionOptions { Temperature = 0.3f };
            foreach (var tool in tools) options.Tools.Add(tool);

            var invocations = new List<ToolInvocation>();

            for (var i = 0; i < maxIterations; i++)
            {
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options, ct);

                if (completion.FinishReason != ChatFinishReason.ToolCalls || completion.ToolCalls.Count == 0)
                    return new ToolRunResult(ExtractText(completion), invocations);

                // The assistant message carrying the tool calls must precede the tool results.
                messages.Add(new AssistantChatMessage(completion));

                foreach (var call in completion.ToolCalls)
                {
                    var args = call.FunctionArguments?.ToString() ?? "{}";
                    string result;
                    try
                    {
                        result = await executeTool(call.FunctionName, args, ct);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Tool {Tool} failed with args {Args}", call.FunctionName, args);
                        result = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
                    }

                    Serilog.Log.Information("Tool {Tool}({Args}) -> {Result}", call.FunctionName, args, result);
                    invocations.Add(new ToolInvocation(call.FunctionName, args, result));
                    messages.Add(new ToolChatMessage(call.Id, result));
                }
            }

            // Safety net: too many tool rounds -> force a plain answer from what we have.
            options.ToolChoice = ChatToolChoice.CreateNoneChoice();
            ChatCompletion final = await chatClient.CompleteChatAsync(messages, options, ct);
            return new ToolRunResult(ExtractText(final), invocations);
        }

        private static string ExtractText(ChatCompletion completion) =>
            string.Concat(completion.Content
                .Where(p => p.Kind == ChatMessageContentPartKind.Text)
                .Select(p => p.Text)).Trim();
    }
}
