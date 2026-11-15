using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using Rag.Data.Entity;
using Rag.Service.Factory.Interfaces;
using Rag.Service.Services.Interfaces;
using RagLib.Embeddings;
using RagLib.Models;
using RagLib.Storage;
using RagWebAPI.Attributes;
using RagWebAPI.Context;
using RagWebAPI.Middleware;
using RagWebAPI.Requests;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using ILogger = Serilog.ILogger;


namespace RagWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueryController : ControllerBase
    {
        ILogger _logger;
        IEmbeddingClient _embeddingClient;
        VectorStore _store;
        AppConfig cfg;
        ConcurrentDictionary<string, ContextWindow> _sessions;
        int modelMaxTokens;
        int responseReserve;
        string openAiKey;
        string openAiModel;
        string systemPrompt;
        IHistoryService _historyService;
        private readonly IServiceAgentService _serviceAgentService;
        private readonly ILlmProviderFactory _llmProviderFactory;
        ITokenValidationService _tokenValidationService;

        public QueryController(ILogger logger, 
            IEmbeddingClient embeddingClient,
            VectorStore store,
            AppConfig appConfig,
            ConcurrentDictionary<string, ContextWindow> sessions,
            IHistoryService historyService,
            IServiceAgentService serviceAgentService,
            ILlmProviderFactory llmProviderFactory)
            ITokenValidationService tokenValidationService)
        {
            _logger = logger;
            _embeddingClient = embeddingClient;
            _store = store;
            cfg = appConfig;
            _sessions = sessions;
            modelMaxTokens = int.TryParse(Environment.GetEnvironmentVariable("MODEL_MAX_TOKENS"), out var _mx) ? _mx : 128_000;
            responseReserve = int.TryParse(Environment.GetEnvironmentVariable("RESPONSE_RESERVE"), out var _rr) ? _rr : 1024;
            openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "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";
            openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
            systemPrompt = "# ROLE\r\nYou are an Ararat Bank customer support agent providing information about banking products and services.\r\n\r\n# KNOWLEDGE BASE CONSTRAINTS\r\n- Answer ONLY using information explicitly stated in the provided context documents\r\n- If the answer is not in the retrieved context, respond with: \"I don't have access to that information in our current documentation. For assistance, please contact Ararat Bank's customer service at [PHONE] or email [EMAIL].\"\r\n- NEVER generate answers from general knowledge, assumptions, or external information\r\n- If context is partial or ambiguous, acknowledge limitations explicitly\r\n\r\n# RESPONSE PROTOCOL\r\n1. **Accuracy First**: Quote or paraphrase directly from source documents\r\n2. **Cite When Relevant**: Reference specific product names, terms, or policy sections\r\n3. **Clarify Before Answering**: If the question is ambiguous, ask 1-2 clarifying questions\r\n4. **Structured Responses**: Use clear formatting for complex information (terms, rates, requirements)\r\n5. **Conversational Tone**: Professional yet approachable - avoid robotic language\r\n\r\n# FORMATTING GUIDELINES\r\n- **Always use Markdown** for well-structured, readable responses\r\n- Use **bold** for product names, important terms, and key information\r\n- Use bullet points (•) or numbered lists for multiple items\r\n- Use headers (##, ###) to organize complex responses with multiple sections\r\n- Use tables for comparing rates, fees, or product features\r\n- Use > blockquotes for important notices or disclaimers\r\n- Use emojis strategically to enhance readability and tone:\r\n  - ✅ for confirmations, available features, or positive information\r\n  - 📋 for requirements or documentation\r\n  - 💰 for rates, fees, or financial information\r\n  - 📞 for contact information\r\n  - ⚠️ for warnings or important notices\r\n  - 🏦 for branch or location information\r\n  - 💳 for card-related information\r\n  - ⏰ for time-sensitive or deadline information\r\n  - ❌ for unavailable features or restrictions\r\n  - ℹ️ for general information\r\n- Keep emoji use professional and not excessive (1-3 per response maximum)\r\n\r\n# SCOPE BOUNDARIES\r\n**IN SCOPE:**\r\n- Banking products and services described in the knowledge base\r\n- Account types, features, requirements, and terms\r\n- Interest rates, fees, and conditions from documentation\r\n- Application and enrollment procedures\r\n- Branch locations, hours, contact methods (if documented)\r\n- General product information available in context\r\n\r\n**OUT OF SCOPE - Redirect to Call Center:**\r\n- Account-specific inquiries (balances, transactions, account status)\r\n- Real-time transaction processing or authorization\r\n- Investment advice or personalized financial planning\r\n- Technical issues with online/mobile banking systems\r\n- Dispute resolution, fraud claims, or formal complaints\r\n- Regulatory, legal, or tax advice\r\n- Services or products not mentioned in the knowledge base\r\n- Password resets or account access issues\r\n\r\n# PROHIBITED BEHAVIORS\r\n- ❌ Do not invent product features, rates, fees, or terms\r\n- ❌ Do not provide outdated information - if unsure of currency, flag it\r\n- ❌ Do not discuss competitors or make comparative statements\r\n- ❌ Do not handle transactions, account modifications, or approvals\r\n- ❌ Do not provide tax, legal, or investment advice\r\n- ❌ Do not make promises about approval, processing times, or outcomes unless explicitly stated in documentation\r\n- ❌ Do not retrieve or discuss other customers' information\r\n\r\n# QUALITY CHECKS\r\nBefore responding, verify:\r\n- [ ] Is this information directly from the retrieved context?\r\n- [ ] Am I staying within documented products/services?\r\n- [ ] Have I avoided speculation or assumptions?\r\n- [ ] Is the customer clearly directed to next steps if needed?\r\n- [ ] Is the response well-formatted with appropriate Markdown?\r\n\r\n# EXAMPLE INTERACTION PATTERNS\r\n\r\n**Good Response with Formatting:**\r\n\"## 💰 Savings Account Interest Rates\r\n\r\nBased on our documentation, Ararat Bank offers the following savings account options:\r\n\r\n| Account Type | Annual Interest Rate | Minimum Balance |\r\n|--------------|---------------------|-----------------|\r\n| **Standard Savings** | 5.5% | 50,000 AMD |\r\n| **Premium Savings** | 7.2% | 500,000 AMD |\r\n| **Youth Savings** | 6.0% | 50,000 AMD |\r\n\r\n✅ Interest is calculated daily and credited monthly.\r\n\r\nWould you like information about opening requirements for any of these accounts?\"\r\n\r\n**Appropriate Deflection:**\r\n\"ℹ️ I don't have specific information about that service in my current documentation. \r\n\r\n📞 **For assistance, please contact:**\r\n- Phone: [NUMBER]\r\n- Email: [EMAIL]\r\n- Website: [URL]\r\n\r\nThey'll provide you with the most accurate and up-to-date information.\"\r\n\r\n**Clarifying Question:**\r\n\"I'd be happy to help you find information about our banking products! \r\n\r\nTo provide the most relevant information, could you tell me:\r\n\r\n1. Which type of product are you interested in?\r\n2. What are you looking to accomplish?\r\n\r\nThis will help me direct you to the right information from our documentation.\"\r\n\r\n**Handling Partial Information:**\r\n\"📋 Based on our documentation, I can tell you that **[Product Name]** requires:\r\n\r\n- ✅ Valid government-issued ID\r\n- ✅ Proof of address\r\n- ✅ Minimum initial deposit: [amount]\r\n\r\nHowever, for complete details about [missing information], please contact our customer service team:\r\n\r\n📞 [PHONE] | 🌐 [WEBSITE]\"\r\n\r\n# CONTACT INFORMATION TEMPLATE\r\nWhen directing customers to other channels, format as:\r\n\r\n📞 **Contact Ararat Bank:**\r\n- **Phone:** [NUMBER]\r\n- **Email:** [EMAIL]\r\n- **Website:** [URL]\r\n- **Branch Locator:** [URL]\r\n- **Business Hours:** [HOURS]\r\n\r\n# CONVERSATION GUIDELINES\r\n- Stay focused on the customer's current question\r\n- If the conversation drifts to out-of-scope topics, politely redirect\r\n- Maintain context from earlier in the conversation when relevant\r\n- End responses with helpful next steps or offers to clarify further\r\n- Be patient with customers who may not use technical banking terminology\r\n- Use formatting to make responses scannable and easy to understand\r\n- Use emojis to add warmth while maintaining professionalism";
            _historyService = historyService;
            _serviceAgentService = serviceAgentService;
            _llmProviderFactory = llmProviderFactory;
            _tokenValidationService = tokenValidationService;
        }

        //[HttpPost]
        //[Route("prompt")]
        //public async Task<IActionResult> Prompt(QueryRequest request)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(request.Query) || string.IsNullOrWhiteSpace(request.SessionId))
        //            return BadRequest(new { error = "Query and SessionId required" });



        //        var vec = await _embeddingClient.EmbedAsync(request.Query, default);
        //        var ctx = _sessions.GetOrAdd(request.SessionId!, _ => new ContextWindow(systemPrompt, modelMaxTokens, responseReserve));

        //        var qVec = await _embeddingClient.EmbedAsync(request.Query, default);
        //        var matches = await _store.SearchAsync(qVec, /*request.TopK ??*/ 5, default);
        //        var contextBlock = string.Join("\n---\n", matches.Select(m => m.Content));
        //        if (!string.IsNullOrWhiteSpace(contextBlock))
        //            ctx.AddContextBlock("Relevant snippets", contextBlock);
        //        ctx.AddUser(request.Query);
        //        ctx.FitToWindow();

        //        if (string.IsNullOrWhiteSpace(openAiKey))
        //            return Ok(new { answer = "No LLM configured.", matches, turns = ctx.Messages.Count });

        //        var client = new OpenAIClient(apiKey: openAiKey);
        //        var chatClient = client.GetChatClient(openAiModel);
        //        ChatCompletion completion = await chatClient.CompleteChatAsync(ctx.Messages, new ChatCompletionOptions { Temperature = (float)0.2 });
        //        var answer = completion.Content[0].Text;
        //        ctx.AddAssistant(answer);

        //        return Ok(new { answer, matches, turns = ctx.Messages.Count });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex, "Error on Prompt");

        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpPost]
        [Route("TelegramPrompt")]
        public async Task<IActionResult> TelegramPrompt(TelegramQueryRequest request)
        {
            var ct = HttpContext.RequestAborted;
            try
            {
                DateTime questionDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                if (string.IsNullOrWhiteSpace(request.Query) || string.IsNullOrWhiteSpace(request.SessionId))
                    return BadRequest(new { error = "Query and SessionId required" });



                var vec = await _embeddingClient.EmbedAsync(request.Query, default);               
                var convo = await _historyService.GetOrCreateAsync(request.UserId, request.SessionId);
              
                var agent = await _serviceAgentService.GetEnabledServiceAgentAsync();
                if (agent == null)
                {
                    await HttpContext.Response.WriteAsync("{\"error\":\"Invalid ServiceAgent\"}\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                    return BadRequest(new { error = "Invalid ServiceAgent" });
                }
                var ctx = await ContextWindow.CreateFromDbAsync(
                                    _historyService,
                                    convo.Id,
                                    systemPrompt,
                                    modelMaxTokens,
                                    responseReserve,
                                    maxTurnsToLoad: 12,
                                    ct);

                var qVec = await _embeddingClient.EmbedAsync(request.Query, default);
                var matches = await _store.SearchAsync(qVec, /*request.TopK ??*/ 5, default);
                var contextBlock = string.Join("\n---\n", matches.Select(m => m.Content));
                if (!string.IsNullOrWhiteSpace(contextBlock))
                    ctx.AddContextBlock("Relevant snippets", contextBlock);
                ctx.AddUser(request.Query);
                ctx.FitToWindow();

                if (string.IsNullOrWhiteSpace(openAiKey))
                    return Ok(new { answer = "No LLM configured.", matches, turns = ctx.Messages.Count });

                var client = new OpenAIClient(apiKey: openAiKey);
                var chatClient = client.GetChatClient(openAiModel);
                ChatCompletion completion = await chatClient.CompleteChatAsync(ctx.Messages, new ChatCompletionOptions { Temperature = (float)0.2 });
                var answer = completion.Content[0].Text;
                ctx.AddAssistant(answer);
                // Finalize turn
                DateTime answerDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

                // Persist the turn
                var turnIndex = await _historyService.GetNextTurnIndexAsync(convo.Id);
                var chunkIds = matches.Select(m => m.Id).Where(id => id != null).Select(id => id!.ToString());
                var turn = await _historyService.SaveTurnAsync(convo.Id, turnIndex, request.Query, answer, chunkIds!, questionDateTime, answerDateTime);

                // Update title after each Q&A
                var newTitle = await GenerateWithoutMarkupAsync(ctx.Messages, agent.ModelName, agent.APIToken, ct);
                await _historyService.UpdateTitleAsync(convo.Id, newTitle);
                return Ok(new { answer, matches, turns = ctx.Messages.Count });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on Prompt");

                return BadRequest(ex.Message);
            }
        }


        //[HttpPost]
        //[Route("prompt/stream")]
        //public async Task StreamPrompt([FromBody] QueryRequest request)
        //{
        //    // Prepare streaming response
        //    HttpContext.Response.Headers["Cache-Control"] = "no-cache";
        //    HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        //    HttpContext.Response.Headers["X-Accel-Buffering"] = "no"; // for nginx, avoid buffering
        //    HttpContext.Response.Headers["Connection"] = "keep-alive";
        //    HttpContext.Response.ContentType = "application/x-ndjson; charset=utf-8";

        //    var ct = HttpContext.RequestAborted;

        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(request.Query) || string.IsNullOrWhiteSpace(request.SessionId))
        //        {
        //            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        //            await HttpContext.Response.WriteAsync("{\"error\":\"Query and SessionId required\"}\n", ct);
        //            await HttpContext.Response.Body.FlushAsync(ct);
        //            return;
        //        }

        //        // Build context (same as your original)
        //        var ctx = _sessions.GetOrAdd(request.SessionId!, _ => new ContextWindow(systemPrompt, modelMaxTokens,responseReserve));

        //        var qVec = await _embeddingClient.EmbedAsync(request.Query, ct);
        //        var matches = await _store.SearchAsync(qVec, 5, ct);
        //        var contextBlock = string.Join("\n---\n", matches.Select(m => m.Content));
        //        if (!string.IsNullOrWhiteSpace(contextBlock))
        //            ctx.AddContextBlock("Relevant snippets", contextBlock);

        //        ctx.AddUser(request.Query);
        //        ctx.FitToWindow();

        //        if (string.IsNullOrWhiteSpace(openAiKey))
        //        {
        //            await HttpContext.Response.WriteAsync("{\"info\":\"No LLM configured.\"}\n", ct);
        //            await HttpContext.Response.Body.FlushAsync(ct);
        //            return;
        //        }

        //        // Start the response stream explicitly (helps some hosts)
        //        await HttpContext.Response.StartAsync(ct);

        //        var client = new OpenAIClient(apiKey: openAiKey);
        //        var chatClient = client.GetChatClient(openAiModel);

        //        var options = new OpenAI.Chat.ChatCompletionOptions
        //        {
        //            Temperature = 0.2f
        //        };

        //        // Accumulate final text to save in history after streaming
        //        var fullText = new System.Text.StringBuilder();

        //        await foreach (OpenAI.Chat.StreamingChatCompletionUpdate update
        //            in chatClient.CompleteChatStreamingAsync(ctx.Messages, options, ct))
        //        {
        //            // Stream newly generated text parts
        //            if (update.ContentUpdate is { Count: > 0 })
        //            {
        //                foreach (var part in update.ContentUpdate)
        //                {
        //                    if (part.Kind == OpenAI.Chat.ChatMessageContentPartKind.Text &&
        //                        !string.IsNullOrEmpty(part.Text))
        //                    {
        //                        fullText.Append(part.Text);

        //                        // Send one NDJSON line per delta
        //                        var jsonLine = System.Text.Json.JsonSerializer.Serialize(new { delta = part.Text });
        //                        await HttpContext.Response.WriteAsync(jsonLine + "\n", ct);
        //                        await HttpContext.Response.Body.FlushAsync(ct);
        //                    }
        //                }
        //            }

        //            // (optional) You could inspect update.RoleUpdate / update.ResponseUpdate here if needed
        //        }

        //        // Done marker (lets the client stop reading cleanly)
        //        await HttpContext.Response.WriteAsync("{\"done\":true}\n", ct);
        //        await HttpContext.Response.Body.FlushAsync(ct);

        //        // Save the final assistant message in your context AFTER streaming completes
        //        var answer = fullText.ToString();
        //        ctx.AddAssistant(answer);

        //        // (optional) trailing metadata (turns, matches)
        //        var meta = System.Text.Json.JsonSerializer.Serialize(new { turns = ctx.Messages.Count, matches });
        //        await HttpContext.Response.WriteAsync(meta + "\n", ct);
        //        await HttpContext.Response.Body.FlushAsync(ct);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        // Client disconnected or request aborted — nothing else to do
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex, "Error on StreamPrompt");
        //        try
        //        {
        //            var err = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        //            await HttpContext.Response.WriteAsync(err + "\n", HttpContext.RequestAborted);
        //            await HttpContext.Response.Body.FlushAsync(HttpContext.RequestAborted);
        //        }
        //        catch
        //        {
        //            // connection might already be closed
        //        }
        //    }
        //}

        [HttpPost]
        [Route("prompt/stream")]
        public async Task StreamPromptWithHihstory([FromBody] QueryRequest request)
        {
            HttpContext.Response.Headers["Cache-Control"] = "no-cache";
            HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            HttpContext.Response.Headers["X-Accel-Buffering"] = "no";
            HttpContext.Response.Headers["Connection"] = "keep-alive";
            HttpContext.Response.ContentType = "application/x-ndjson; charset=utf-8";

            var ct = HttpContext.RequestAborted;

            try
            {
                DateTime questionDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                if (string.IsNullOrWhiteSpace(request.Query) || string.IsNullOrWhiteSpace(request.SessionId))
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await HttpContext.Response.WriteAsync("{\"error\":\"Query and SessionId required\"}\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                    return;
                }

                var uid = HttpContext.GetUserId();
               
                var convo = await _historyService.GetOrCreateAsync(uid, request.SessionId);

                var agent = await _serviceAgentService.GetEnabledServiceAgentAsync();
                if (agent == null)
                {
                    await HttpContext.Response.WriteAsync("{\"error\":\"Invalid ServiceAgent\"}\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                    return;
                }

                var ctx = await ContextWindow.CreateFromDbAsync(
                                    _historyService,
                                    convo.Id,
                                    agent.SystemPrompt,
                                    modelMaxTokens,
                                    responseReserve,
                                    maxTurnsToLoad: 12,
                                    ct);

                var qVec = await _embeddingClient.EmbedAsync(request.Query, ct);
                var matches = await _store.SearchAsync(qVec, 5, ct);

                // Build context
                var contextBlock = string.Join("\n---\n", matches.Select(m => m.Content));
                if (!string.IsNullOrWhiteSpace(contextBlock))
                    ctx.AddContextBlock("Relevant snippets", contextBlock);

                ctx.AddUser(request.Query);
                ctx.FitToWindow();

                if (string.IsNullOrWhiteSpace(agent.APIToken))
                {
                    await HttpContext.Response.WriteAsync("{\"info\":\"No LLM configured.\"}\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                    return;
                }

                await HttpContext.Response.StartAsync(ct);

                var llm = _llmProviderFactory.Create(agent);

                var fullText = new System.Text.StringBuilder();

                await foreach (var delta in llm.StreamAsync(ctx.Messages, ct))
                {
                    fullText.Append(delta);
                    var jsonLine = System.Text.Json.JsonSerializer.Serialize(new { delta });
                    await HttpContext.Response.WriteAsync(jsonLine + "\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                }

                await HttpContext.Response.WriteAsync("{\"done\":true}\n", ct);
                await HttpContext.Response.Body.FlushAsync(ct);

                // Finalize turn
                var answer = fullText.ToString();
                DateTime answerDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                ctx.AddAssistant(answer);

                // Persist the turn
                var turnIndex = await _historyService.GetNextTurnIndexAsync(convo.Id);
                var chunkIds = matches.Select(m => m.Id).Where(id => id != null).Select(id => id!.ToString());
                var chunkIdsWithUrls = matches.Select(m => new { m.Images, m.Id }).ToList();
                var turn = await _historyService.SaveTurnAsync(convo.Id, turnIndex, request.Query, answer, chunkIds!,questionDateTime,answerDateTime);

                // Update title after each Q&A
                var newTitle = await GenerateWithoutMarkupAsync(ctx.Messages,agent.ModelName,agent.APIToken, ct);
                await _historyService.UpdateTitleAsync(convo.Id, newTitle);

                // Optional tail meta for client
                var meta = System.Text.Json.JsonSerializer.Serialize(new
                {
                    conversationId = convo.Id,
                    turnIndex,
                    title = newTitle,
                    turnId = turn.Id,
                    chunkIdsWithUrls = chunkIdsWithUrls
                });
                await HttpContext.Response.WriteAsync(meta + "\n", ct);
                await HttpContext.Response.Body.FlushAsync(ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on StreamPrompt");
                try
                {
                    var err = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
                    await HttpContext.Response.WriteAsync(err + "\n", HttpContext.RequestAborted);
                    await HttpContext.Response.Body.FlushAsync(HttpContext.RequestAborted);
                }
                catch { }
            }
        }

        [HttpPost]
        [Route("prompt/streamnew")]
        public async Task StreamPromptWithHihstoryNew([FromBody] QueryRequest request)
        {
            // Streaming-friendly headers
            HttpContext.Response.Headers["Cache-Control"] = "no-cache";
            HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            HttpContext.Response.Headers["X-Accel-Buffering"] = "no"; // avoid nginx buffering
            HttpContext.Response.Headers["Connection"] = "keep-alive";
            HttpContext.Response.ContentType = "application/x-ndjson; charset=utf-8";

            var ct = HttpContext.RequestAborted;

            try
            {
                var questionDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

                if (string.IsNullOrWhiteSpace(request.Query) || string.IsNullOrWhiteSpace(request.SessionId))
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await HttpContext.Response.WriteAsync("{\"error\":\"Query and SessionId required\"}\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                    return;
                }

                var uid = HttpContext.GetUserId();
                var convo = await _historyService.GetOrCreateAsync(uid, request.SessionId);
                var agent = await _serviceAgentService.GetEnabledServiceAgentAsync();
                if (agent == null)
                {
                    await HttpContext.Response.WriteAsync("{\"error\":\"Invalid ServiceAgent\"}\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                    return;
                }
                var ctx = await ContextWindow.CreateFromDbAsync(
                    _historyService,
                    convo.Id,
                    systemPrompt,
                    modelMaxTokens,
                    responseReserve,
                    maxTurnsToLoad: 12,
                    ct);

                // RAG matches
                var qVec = await _embeddingClient.EmbedAsync(request.Query, ct);
                var matches = await _store.SearchAsync(qVec, 5, ct);

                // Add text snippets to context
                var contextBlock = string.Join("\n---\n", matches.Select(m => m.Content));
                if (!string.IsNullOrWhiteSpace(contextBlock))
                    ctx.AddContextBlock("Relevant snippets", contextBlock);

                // Build a flat image URL list from matches (normalize to absolute)
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                string? NormalizeUrl(string? raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return null;
                    var u = raw.Replace('\\', '/'); // fix Windows slashes
                    if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                        u.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        return u;
                    if (u.StartsWith("/")) return baseUrl.TrimEnd('/') + u;
                    return baseUrl.TrimEnd('/') + "/" + u.TrimStart('/');
                }

                var imageUrls = matches
                    .SelectMany(m => (IEnumerable<string>)(m.Images ?? Enumerable.Empty<string>()))
                    .Select(NormalizeUrl)
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()!;

                // Strong instruction: model must place the exact URL in the marker after the sentence that uses it
                // Example: “...the receipt looks like this.{{IMGURL:https://host/img/abc.png}}”
                if (imageUrls.Count > 0)
                {
                    var listing = string.Join("\n", imageUrls.Select(u => $"- {u}"));
                    ctx.AddContextBlock(
                        "Available images (use these exact URLs)",
                        "When you refer to any of these images, insert a marker immediately after the sentence:\n" +
                        "Format: {{IMGURL:<absolute-url>}}\n" +
                        "Example: The receipt looks like this.{{IMGURL:https://host/path/img.png}}\n\n" +
                        listing);
                }

                ctx.AddUser(request.Query);
                ctx.FitToWindow();

                if (string.IsNullOrWhiteSpace(openAiKey))
                {
                    await HttpContext.Response.WriteAsync("{\"info\":\"No LLM configured.\"}\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                    return;
                }

                await HttpContext.Response.StartAsync(ct);

                // Track duplicates so we don't re-emit the same image
                var emittedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var client = new OpenAIClient(apiKey: openAiKey);
                var chatClient = client.GetChatClient(openAiModel);
                var options = new OpenAI.Chat.ChatCompletionOptions { Temperature = 0.2f };

                var fullText = new System.Text.StringBuilder();
                var pending = new System.Text.StringBuilder();

                // Regex for {{IMGURL:<anything-but-}>}}
                // We capture everything up to the closing "}}" (greedy but bounded)
                var imgUrlTag = new System.Text.RegularExpressions.Regex(@"\{\{IMGURL:([^}]+)\}\}",
                    System.Text.RegularExpressions.RegexOptions.Compiled);

                // Keep a tiny tail so we don't split "{{IMGURL:"
                const int HOLD_BACK = 16;

                async Task EmitTextAsync(string text)
                {
                    if (string.IsNullOrEmpty(text)) return;
                    fullText.Append(text);
                    var json = System.Text.Json.JsonSerializer.Serialize(new { type = "delta", delta = text });
                    await HttpContext.Response.WriteAsync(json + "\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                }

                async Task EmitImageAsync(string? rawUrl)
                {
                    var url = NormalizeUrl(rawUrl);
                    if (string.IsNullOrWhiteSpace(url)) return;
                    if (!emittedImages.Add(url)) return;

                    var md = $"![Image]({url})\n";
                    fullText.Append(md);
                    var imgDelta = System.Text.Json.JsonSerializer.Serialize(new { type = "delta", delta = md });
                    await HttpContext.Response.WriteAsync(imgDelta + "\n", ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                }

                await foreach (var update in chatClient.CompleteChatStreamingAsync(ctx.Messages, options, ct))
                {
                    if (update.ContentUpdate is not { Count: > 0 }) continue;

                    foreach (var part in update.ContentUpdate)
                    {
                        if (part.Kind != OpenAI.Chat.ChatMessageContentPartKind.Text ||
                            string.IsNullOrEmpty(part.Text))
                            continue;

                        // 1) Append to buffer
                        pending.Append(part.Text);

                        // 2) Process any complete IMGURL markers now
                        while (true)
                        {
                            var buffer = pending.ToString();
                            var match = imgUrlTag.Match(buffer);
                            if (!match.Success) break;

                            // Emit the text BEFORE the marker
                            var before = buffer.Substring(0, match.Index);
                            await EmitTextAsync(before);

                            // Remove processed prefix + the marker
                            pending.Remove(0, match.Index + match.Length);

                            // Emit the image right here
                            var rawUrl = match.Groups[1].Value.Trim();
                            await EmitImageAsync(rawUrl);
                        }

                        // 3) Stream out safe text; keep a tiny tail to avoid splitting a future "{{IMGURL:"
                        if (pending.Length > HOLD_BACK)
                        {
                            var safeLen = pending.Length - HOLD_BACK;
                            var tailLen = pending.Length - safeLen;
                            var tail = pending.ToString(safeLen, tailLen);

                            // crude guard: if we see an opening "{{IMGURL" in tail, keep from there
                            var markerIdx = tail.LastIndexOf("{{IMGURL", StringComparison.Ordinal);
                            if (markerIdx > -1)
                                safeLen += markerIdx;

                            if (safeLen > 0)
                            {
                                var safeText = pending.ToString(0, safeLen);
                                await EmitTextAsync(safeText);
                                pending.Remove(0, safeLen);
                            }
                        }
                    }
                }

                // 4) Flush any leftover text
                if (pending.Length > 0)
                {
                    await EmitTextAsync(pending.ToString());
                    pending.Clear();
                }

                // Done marker
                await HttpContext.Response.WriteAsync("{\"done\":true}\n", ct);
                await HttpContext.Response.Body.FlushAsync(ct);

                // Finalize turn
                var answer = fullText.ToString();
                var answerDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                ctx.AddAssistant(answer);

                // Persist turn
                var turnIndex = await _historyService.GetNextTurnIndexAsync(convo.Id);
                var chunkIds = matches.Select(m => m.Id).Where(id => id != null).Select(id => id!.ToString());
                var chunkIdsWithUrls = matches.Select(m => new { m.Images, m.Id }).ToList();

                var turn = await _historyService.SaveTurnAsync(
                    convo.Id,
                    turnIndex,
                    request.Query,
                    answer,
                    chunkIds!,
                    questionDateTime,
                    answerDateTime);

                // Title
                var newTitle = await GenerateWithoutMarkupAsync(ctx.Messages, agent.ModelName, agent.APIToken, ct);
                await _historyService.UpdateTitleAsync(convo.Id, newTitle);

                // Tail meta
                var meta = System.Text.Json.JsonSerializer.Serialize(new
                {
                    conversationId = convo.Id,
                    turnIndex,
                    title = newTitle,
                    turnId = turn.Id,
                    chunkIdsWithUrls
                });
                await HttpContext.Response.WriteAsync(meta + "\n", ct);
                await HttpContext.Response.Body.FlushAsync(ct);
            }
            catch (OperationCanceledException) { /* client disconnected */ }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on StreamPrompt");
                try
                {
                    var err = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
                    await HttpContext.Response.WriteAsync(err + "\n", HttpContext.RequestAborted);
                    await HttpContext.Response.Body.FlushAsync(HttpContext.RequestAborted);
                }
                catch { /* connection may be closed */ }
            }
        }



        // --- helpers ---

        private static string GetBaseUrl(HttpRequest req)
        {
            // Builds "https://host[:port]" to prefix file-systemish paths
            var scheme = req.Scheme;
            var host = req.Host.HasValue ? req.Host.Value : "localhost";
            return $"{scheme}://{host}";
        }

        private static string? NormalizeImageUrl(string? raw, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            // Fix Windows-style backslashes from your sample: "011125\\abc.png"
            var u = raw.Replace('\\', '/');

            // If it already looks absolute (http/https/data), keep it
            if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                u.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return u;

            // If it starts with /, prefix with base URL
            if (u.StartsWith("/"))
                return baseUrl.TrimEnd('/') + u;

            // Otherwise make it absolute by prefixing "/"
            return baseUrl.TrimEnd('/') + "/" + u.TrimStart('/');
        }


        private async Task<string> GenerateAsync(IEnumerable<OpenAI.Chat.ChatMessage> messages, CancellationToken ct)
        {
            var _client = new OpenAIClient(apiKey: openAiKey);
            var chat = _client.GetChatClient(openAiModel);
            var prompt = OpenAI.Chat.ChatMessage.CreateSystemMessage(
                "Generate a concise 5–8 word title for this conversation so far. No punctuation at end.");
            var last10 = messages.TakeLast(10).ToList();

            var res = await chat.CompleteChatAsync([prompt, .. last10], new OpenAI.Chat.ChatCompletionOptions { Temperature = 0.2f }, ct);
            var title = res.Value.Content[0].Text?.Trim();
            return string.IsNullOrWhiteSpace(title) ? "New conversation" : title;
        }

        private async Task<string> GenerateWithoutMarkupAsync(IEnumerable<OpenAI.Chat.ChatMessage> messages,string modelName, string APIToken, CancellationToken ct)
        {
            var _client = new OpenAIClient(apiKey: APIToken);
            var chat = _client.GetChatClient(modelName);

            // Limit conversation context to last 10 messages
            var last10 = messages.TakeLast(10).ToList();

            // Use a better system prompt similar to ChatGPT's internal behavior
            var systemPrompt = OpenAI.Chat.ChatMessage.CreateSystemMessage(
                "You are an assistant that creates clean, natural titles for chats. " +
                "Generate a concise, human-like conversation title based on the dialogue so far. " +
                "Avoid quotes, markdown, emojis, or special characters. " +
                "Keep it short — ideally under 8 words. " +
                "Return only the title text, nothing else."
            );

            // Generate title
            var res = await chat.CompleteChatAsync(
                [systemPrompt, .. last10],
                new OpenAI.Chat.ChatCompletionOptions
                {
                    Temperature = 0.7f  // slightly more creative for natural phrasing
                },
                ct
            );

            var title = res.Value.Content.FirstOrDefault()?.Text?.Trim();

            // Clean up just in case (remove quotes, markdown, etc.)
            if (!string.IsNullOrWhiteSpace(title))
            {
                title = title
                    .Trim('"', '\'', '“', '”', '.', '!', '?')
                    .Replace("*", "")
                    .Replace("#", "")
                    .Replace("`", "")
                    .Trim();
            }

            return string.IsNullOrWhiteSpace(title) ? "New conversation" : title;
        }


        /// <summary>
        /// This version guarantees that the same set of chat messages will always produce the same title — no randomness, no temperature drift — just clean, repeatable results, just like ChatGPT’s internal title generation does after a conversation ends.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<string> GenerateDeterministicAsync(IEnumerable<OpenAI.Chat.ChatMessage> messages, CancellationToken ct)
        {
            var _client = new OpenAIClient(apiKey: openAiKey);
            var chat = _client.GetChatClient(openAiModel);

            // Use the last 10 messages for context
            var last10 = messages.TakeLast(10).ToList();

            var systemPrompt = OpenAI.Chat.ChatMessage.CreateSystemMessage(
                "You are a helpful assistant that generates very short, natural conversation titles. " +
                "Produce a concise descriptive title in 4 to 8 words. " +
                "Do not include punctuation at the end, quotes, emojis, markdown, or special characters. " +
                "Return only the title text, nothing else."
            );

            var input = new List<OpenAI.Chat.ChatMessage> { systemPrompt };
            input.AddRange(last10);

            // Build options only with fields commonly present. Omit any MaxTokens-style fields.
            var options = new OpenAI.Chat.ChatCompletionOptions
            {
                // Deterministic output when supported:
                Temperature = 0.0f,
                TopP = 1.0f
                // NOTE: intentionally not setting MaxTokens/MaxOutputTokens to stay compatible
            };

            var res = await chat.CompleteChatAsync(input, options, ct);

            // Safe path to get text (null-safe)
            var raw = res?.Value?.Content?.FirstOrDefault()?.Text ?? string.Empty;
            var title = raw.Trim();

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Remove surrounding quotes and trailing punctuation
                title = title.Trim().Trim('"', '\'', '“', '”', '.', '!', '?');

                // Remove common markdown/symbol characters keep letters/numbers/punctuation inside words
                title = title.Replace("*", "")
                             .Replace("#", "")
                             .Replace("`", "")
                             .Replace("•", "")
                             .Replace("—", " ")
                             .Trim();

                // Normalize whitespace
                title = System.Text.RegularExpressions.Regex.Replace(title, @"\s+", " ").Trim();

                // Split into words and enforce a deterministic fixed limit (e.g., 6 words)
                const int fixedWordLimit = 6; // change to 4..8 to match preferred title length
                var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length == 0)
                    return "New conversation";

                if (words.Length > fixedWordLimit)
                    title = string.Join(" ", words.Take(fixedWordLimit));
                else
                    title = string.Join(" ", words);

                // Capitalize first letter for consistency
                if (title.Length > 0)
                    title = char.ToUpperInvariant(title[0]) + (title.Length > 1 ? title.Substring(1) : string.Empty);
            }

            return string.IsNullOrWhiteSpace(title) ? "New conversation" : title;
        }

        [SkipUserIdHeader]
        [HttpGet("prompt/stream2")]
        public async Task StreamPromptWithHistory2([FromQuery] string query, [FromQuery] string sessionId, [FromQuery] string token)
        {
            var ct = HttpContext.RequestAborted;

            HttpContext.Response.Headers["Cache-Control"] = "no-cache";
            HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            HttpContext.Response.Headers["X-Accel-Buffering"] = "no";
            HttpContext.Response.Headers["Connection"] = "keep-alive";
            HttpContext.Response.ContentType = "text/event-stream; charset=utf-8";

            try
            {
                if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(sessionId))
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await SendEventAsync("error", "query and sessionId are required",ct);
                    return;
                }

                var isValidToken = await _tokenValidationService.ValidateTokenAsync(token);
                
                var uid = isValidToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var convo = await _historyService.GetOrCreateAsync(uid, sessionId);

                var ctx = await ContextWindow.CreateFromDbAsync(
                                    _historyService,
                                    convo.Id,
                                    systemPrompt,
                                    modelMaxTokens,
                                    responseReserve,
                                    maxTurnsToLoad: 12,
                                    ct);

                var qVec = await _embeddingClient.EmbedAsync(query, ct);
                var matches = await _store.SearchAsync(qVec, 5, ct);

                var contextBlock = string.Join("\n---\n", matches.Select(m => m.Content));
                if (!string.IsNullOrWhiteSpace(contextBlock))
                    ctx.AddContextBlock("Relevant snippets", contextBlock);

                ctx.AddUser(query);
                ctx.FitToWindow();

                
                await HttpContext.Response.StartAsync(ct);

                var client = new OpenAIClient(apiKey: openAiKey);
                var chatClient = client.GetChatClient(openAiModel);
                var options = new OpenAI.Chat.ChatCompletionOptions { Temperature = 0.2f };

                var fullText = new StringBuilder();

                await foreach (var update in chatClient.CompleteChatStreamingAsync(ctx.Messages, options, ct))
                {
                    if (ct.IsCancellationRequested) break;

                    if (update.ContentUpdate is { Count: > 0 })
                    {
                        foreach (var part in update.ContentUpdate)
                        {
                            if (part.Kind == OpenAI.Chat.ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
                            {
                                fullText.Append(part.Text);
                                await SendEventAsync("token", part.Text,ct);
                            }
                        }
                    }
                }

                var answer = fullText.ToString();
                ctx.AddAssistant(answer);

                var questionDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                var answerDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

                var turnIndex = await _historyService.GetNextTurnIndexAsync(convo.Id);
                var chunkIds = matches.Select(m => m.Id).Where(id => id != null).Select(id => id!.ToString());
                var turn = await _historyService.SaveTurnAsync(convo.Id, turnIndex, query, answer, chunkIds!, questionDateTime, answerDateTime);

                var newTitle = await GenerateWithoutMarkupAsync(ctx.Messages, ct);
                await _historyService.UpdateTitleAsync(convo.Id, newTitle);

                var meta = System.Text.Json.JsonSerializer.Serialize(new
                {
                    conversationId = convo.Id,
                    turnIndex,
                    title = newTitle,
                    turnId = turn.Id,
                    chunkIds = chunkIds
                });

                await SendEventAsync("meta", meta, ct);
                await SendEventAsync("done", "true",ct);
            }
            catch (OperationCanceledException)
            {
                _logger.Information("SSE client disconnected");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in SSE stream");
                try
                {
                    await SendEventAsync("error", ex.Message, ct);
                }
                catch { }
            }
        }
        private async Task SendEventAsync(string eventName, string data, CancellationToken ct)
        {
            await HttpContext.Response.WriteAsync($"event: {eventName}\n", ct);

            if (!string.IsNullOrEmpty(data))
            {
                foreach (var line in data.Split('\n'))
                {
                    await HttpContext.Response.WriteAsync($"data: {line.TrimEnd('\r')}\n", ct);
                }
            }
            else
            {
                await HttpContext.Response.WriteAsync("data:\n", ct);
            }

            await HttpContext.Response.WriteAsync("\n", ct);
            await HttpContext.Response.Body.FlushAsync(ct);
        }
    }
}
