using OpenAI.Chat;

namespace Rag.Service.Factory.Interfaces
{
    /// <summary>One tool call the model made during a tool loop (for logging / UI).</summary>
    public record ToolInvocation(string Name, string ArgumentsJson, string ResultJson);

    /// <summary>Final answer text plus every tool call that led to it.</summary>
    public record ToolRunResult(string Text, IReadOnlyList<ToolInvocation> ToolCalls);

    public interface ILlmProvider
    {
        IAsyncEnumerable<string> StreamAsync(IEnumerable<ChatMessage> messages, CancellationToken ct);
        Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct);

        /// <summary>
        /// Function-calling loop: sends <paramref name="messages"/> with <paramref name="tools"/>, executes every
        /// requested tool via <paramref name="executeTool"/> (name, argumentsJson) and feeds results back as
        /// ToolChatMessages until the model returns a final text answer. <paramref name="messages"/> is appended to.
        /// </summary>
        Task<ToolRunResult> CompleteWithToolsAsync(
            List<ChatMessage> messages,
            IReadOnlyList<ChatTool> tools,
            Func<string, string, CancellationToken, Task<string>> executeTool,
            CancellationToken ct,
            int maxIterations = 6);
    }
}
