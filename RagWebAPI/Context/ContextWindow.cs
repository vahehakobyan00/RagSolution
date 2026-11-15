using OpenAI.Chat;
using SharpToken;
using System.Text;

namespace RagWebAPI.Context
{
    using Microsoft.EntityFrameworkCore.Migrations;
    using OpenAI.Chat; // v2 SDK types
    using Rag.Service.Services.Interfaces;
    using System.Text;

    public sealed class ContextWindow
    {
        private readonly int _modelMaxTokens;
        private readonly int _responseReserve;
        private readonly GptEncoding _enc = GptEncoding.GetEncoding("cl100k_base");
        private readonly List<ChatMessage> _history = new();
              
        private ContextWindow(string systemPrompt, int modelMaxTokens, int responseReserve)
        {
            _modelMaxTokens = modelMaxTokens;
            _responseReserve = responseReserve;
            _history.Add(new SystemChatMessage(systemPrompt));
        }

        /// <summary>
        /// Factory: create a window seeded with recent conversation from DB.
        /// </summary>
        public static async Task<ContextWindow> CreateFromDbAsync(
            IHistoryService historyservice,
            long conversationId,
            string systemPrompt,
            int modelMaxTokens = 128_000,
            int responseReserve = 1024,
            int maxTurnsToLoad = 12,                // tweak as you like
            CancellationToken ct = default)
        {
            var cw = new ContextWindow(systemPrompt, modelMaxTokens, responseReserve);

            // Load last N turns, newest first -> reverse to chronological
            var recent = await historyservice.GetRecentTurnsAsync(conversationId, maxTurnsToLoad);
            recent.Reverse();

            foreach (var t in recent)
            {
                if (!string.IsNullOrWhiteSpace(t.Question))
                    cw._history.Add(new UserChatMessage(t.Question));

                if (!string.IsNullOrWhiteSpace(t.Answer))
                    cw._history.Add(new AssistantChatMessage(t.Answer));
            }

            return cw;
        }

        public IReadOnlyList<ChatMessage> Messages => _history;

        public void AddUser(string text) => _history.Add(new UserChatMessage(text));
        public void AddAssistant(string text) => _history.Add(new AssistantChatMessage(text));

        /// <summary>
        /// Adds a system context block (e.g., retrieved snippets) right where you call it.
        /// Call after CreateFromDbAsync and before AddUser(request.Query).
        /// </summary>
        public void AddContextBlock(string title, string content) =>
            _history.Add(new SystemChatMessage($"{title}:\n{content}"));

        private int EstimateTokens(IReadOnlyList<ChatMessage> msgs)
        {
            int total = 0;
            foreach (var m in msgs)
                foreach (var c in m.Content)
                    total += _enc.Encode(c.Text ?? string.Empty).Count;
            return total + msgs.Count * 6;
        }

        /// <summary>
        /// Keeps messages within the model window; summarizes early turns if needed.
        /// </summary>
        public void FitToWindow(bool allowSummarize = true)
        {
            if (_history.Count <= 1) return;

            int limit = _modelMaxTokens - _responseReserve;

            // Trim earliest non-system messages first
            while (EstimateTokens(_history) > limit && _history.Count > 2)
                _history.RemoveAt(1);

            // Summarize a slice if still too big
            if (allowSummarize && EstimateTokens(_history) > limit && _history.Count > 4)
            {
                var slice = _history.GetRange(1, Math.Min(6, _history.Count - 2));
                var sb = new StringBuilder().AppendLine("Conversation summary:");
                foreach (var m in slice)
                {
                    var who = m switch
                    {
                        SystemChatMessage => "System",
                        UserChatMessage => "User",
                        AssistantChatMessage => "Assistant",
                        _ => "Other"
                    };
                    var text = string.Join(" ", m.Content.Select(c => c.Text)).Trim();
                    if (text.Length > 400) text = text[..400] + "...";
                    sb.AppendLine($"- {who}: {text}");
                }
                _history.RemoveRange(1, slice.Count);
                _history.Insert(1, new SystemChatMessage(sb.ToString()));
            }

            // Final trimming pass if required
            while (EstimateTokens(_history) > limit && _history.Count > 3)
                _history.RemoveAt(1);
        }
    }

}
