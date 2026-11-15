using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RagWebAPI.Agent;
using RagWebAPI.Attributes;
using RagWebAPI.Voice;
using ILogger = Serilog.ILogger;

namespace RagWebAPI.Controllers
{
    public class VoiceAskRequest
    {
        /// <summary>Recorded audio (webm/ogg/mp4/wav/mp3) from the browser.</summary>
        public IFormFile Audio { get; set; } = default!;
        public string? SessionId { get; set; }
    }

    public class VoiceTextRequest
    {
        public string Text { get; set; } = default!;
        public string? SessionId { get; set; }
    }

    public record VoiceToolCall(string Name, string Arguments, string Result);

    public record VoiceResponse(
        string Transcript,
        string Answer,
        string? AudioBase64,
        string SessionId,
        string? Language,
        IReadOnlyList<VoiceToolCall> ToolCalls);

    /// <summary>
    /// Push-to-talk restaurant agent: audio -> STT -> agent (tools + KB + history) -> TTS.
    /// [SkipUserIdHeader]: the demo page sends no X-User-Id; history is stored under Voice:UserId + sessionId.
    /// </summary>
    [ApiController]
    [Route("voice")]
    [SkipUserIdHeader]
    public class VoiceController : ControllerBase
    {
        private const long MaxAudioBytes = 25 * 1024 * 1024; // OpenAI upload limit
        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".webm", ".ogg", ".oga", ".mp4", ".m4a", ".mp3", ".mpeg", ".mpga", ".wav", ".flac" };

        private readonly IVoiceService _voice;
        private readonly RestaurantAgent _agent;
        private readonly VoiceOptions _options;
        private readonly ILogger _logger;

        public VoiceController(IVoiceService voice, RestaurantAgent agent, IOptions<VoiceOptions> options, ILogger logger)
        {
            _voice = voice;
            _agent = agent;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>Multipart audio + sessionId -> { transcript, answer, audioBase64 (mp3), sessionId }.</summary>
        [HttpPost("ask")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxAudioBytes)]
        public async Task<IActionResult> Ask([FromForm] VoiceAskRequest request)
        {
            var ct = HttpContext.RequestAborted;
            if (request.Audio is null || request.Audio.Length == 0)
                return BadRequest(new { error = "Audio file is required." });

            var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString() : request.SessionId;

            // The API detects the format from the file extension.
            var ext = Path.GetExtension(request.Audio.FileName);
            var fileName = AudioExtensions.Contains(ext) ? "audio" + ext.ToLowerInvariant() : "audio.webm";

            try
            {
                TranscriptionResult stt;
                await using (var stream = request.Audio.OpenReadStream())
                    stt = await _voice.TranscribeAsync(stream, fileName, ct);

                if (string.IsNullOrWhiteSpace(stt.Text))
                    return UnprocessableEntity(new { error = "No speech detected. Hold the button and speak.", sessionId });

                return Ok(await AnswerAsync(stt.Text, sessionId, stt.Language, ct));
            }
            catch (OperationCanceledException) { return StatusCode(499); }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, "Voice ask failed (configuration)");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message, sessionId });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Voice ask failed");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message, sessionId });
            }
        }

        /// <summary>Same as /voice/ask but with typed text (testing without a microphone).</summary>
        [HttpPost("text")]
        public async Task<IActionResult> Text([FromBody] VoiceTextRequest request)
        {
            var ct = HttpContext.RequestAborted;
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { error = "Text is required." });

            var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString() : request.SessionId;
            try
            {
                return Ok(await AnswerAsync(request.Text.Trim(), sessionId, null, ct));
            }
            catch (OperationCanceledException) { return StatusCode(499); }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, "Voice text failed (configuration)");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message, sessionId });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Voice text failed");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message, sessionId });
            }
        }

        private async Task<VoiceResponse> AnswerAsync(string transcript, string sessionId, string? language, CancellationToken ct)
        {
            var reply = await _agent.AskAsync(_options.UserId, sessionId, transcript, ct);

            // TTS failure should not lose the answer: return text, the page falls back to browser speech.
            string? audioBase64 = null;
            try
            {
                audioBase64 = Convert.ToBase64String(await _voice.SynthesizeMp3Async(reply.Answer, ct));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.Error(ex, "TTS failed; returning text only");
            }

            return new VoiceResponse(
                transcript,
                reply.Answer,
                audioBase64,
                sessionId,
                language,
                reply.ToolCalls.Select(t => new VoiceToolCall(t.Name, t.ArgumentsJson, t.ResultJson)).ToList());
        }
    }
}
