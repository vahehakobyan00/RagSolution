using Rag.Service.Agent;
using Rag.Service.Factory.Interfaces;
using Rag.Service.Services.Interfaces;
using RagLib.Embeddings;
using RagLib.Storage;
using RagWebAPI.Context;
using System.Globalization;
using ILogger = Serilog.ILogger;

namespace RagWebAPI.Agent
{
    public record AgentReply(string Answer, long ConversationId, IReadOnlyList<ToolInvocation> ToolCalls);

    /// <summary>
    /// One user utterance -> one spoken-style answer.
    /// System prompt from the enabled ServiceAgent + current date/time + knowledge-base snippets
    /// + per-session history (HistoryService/ContextWindow) + restaurant tools (menu, availability, bookings).
    /// </summary>
    public class RestaurantAgent
    {
        private const int MaxTurnsToLoad = 12;
        private const int KnowledgeBaseTopK = 4;

        private readonly IServiceAgentService _serviceAgentService;
        private readonly ILlmProviderFactory _llmProviderFactory;
        private readonly IHistoryService _historyService;
        private readonly IEmbeddingClient _embeddingClient;
        private readonly VectorStore _store;
        private readonly RestaurantToolbox _toolbox;
        private readonly ILogger _logger;
        private readonly int _modelMaxTokens;
        private readonly int _responseReserve;

        public RestaurantAgent(
            IServiceAgentService serviceAgentService,
            ILlmProviderFactory llmProviderFactory,
            IHistoryService historyService,
            IEmbeddingClient embeddingClient,
            VectorStore store,
            RestaurantToolbox toolbox,
            ILogger logger)
        {
            _serviceAgentService = serviceAgentService;
            _llmProviderFactory = llmProviderFactory;
            _historyService = historyService;
            _embeddingClient = embeddingClient;
            _store = store;
            _toolbox = toolbox;
            _logger = logger;
            _modelMaxTokens = int.TryParse(Environment.GetEnvironmentVariable("MODEL_MAX_TOKENS"), out var mx) ? mx : 128_000;
            _responseReserve = int.TryParse(Environment.GetEnvironmentVariable("RESPONSE_RESERVE"), out var rr) ? rr : 1024;
        }

        public async Task<AgentReply> AskAsync(string userId, string sessionId, string text, CancellationToken ct)
        {
            var questionDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            var agent = await _serviceAgentService.GetEnabledServiceAgentAsync()
                ?? throw new InvalidOperationException("No enabled ServiceAgent. Run deploy/restaurant-agent.sql or enable one in the admin panel.");

            var convo = await _historyService.GetOrCreateAsync(userId, sessionId);

            var ctx = await ContextWindow.CreateFromDbAsync(
                _historyService, convo.Id, agent.SystemPrompt, _modelMaxTokens, _responseReserve, MaxTurnsToLoad, ct);

            var now = DateTime.Now;
            ctx.AddContextBlock("Current date and time",
                now.ToString("dddd, yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

            // Knowledge base (address, hours, policies...). Optional: the agent still works if Ollama/pgvector is down.
            var chunkIds = new List<string>();
            try
            {
                var qVec = await _embeddingClient.EmbedAsync(text, ct);
                var matches = await _store.SearchAsync(qVec, KnowledgeBaseTopK, ct);
                var snippets = string.Join("\n---\n", matches.Select(m => m.Content));
                if (!string.IsNullOrWhiteSpace(snippets))
                    ctx.AddContextBlock("Restaurant knowledge base", snippets);
                chunkIds.AddRange(matches.Select(m => m.Id.ToString()));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Knowledge base search failed; answering without it");
            }

            ctx.AddUser(text);
            ctx.FitToWindow();

            var llm = _llmProviderFactory.Create(agent);
            var result = await llm.CompleteWithToolsAsync(ctx.Messages.ToList(), _toolbox.Tools, _toolbox.ExecuteAsync, ct);
            var answer = string.IsNullOrWhiteSpace(result.Text) ? "Sorry, could you repeat that?" : result.Text;

            var answerDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var turnIndex = await _historyService.GetNextTurnIndexAsync(convo.Id);
            await _historyService.SaveTurnAsync(convo.Id, turnIndex, text, answer, chunkIds, questionDateTime, answerDateTime);

            // Cheap title (no extra LLM call, keeps voice latency low).
            if (turnIndex == 1)
                await _historyService.UpdateTitleAsync(convo.Id, text.Length > 60 ? text[..60] + "..." : text);

            return new AgentReply(answer, convo.Id, result.ToolCalls);
        }
    }
}
