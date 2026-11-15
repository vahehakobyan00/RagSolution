using Microsoft.Extensions.Options;
using OpenAI.Audio;
using RagLib.Models;
using ILogger = Serilog.ILogger;

namespace RagWebAPI.Voice
{
    /// <summary>Bound from the "Voice" section of appsettings.json.</summary>
    public class VoiceOptions
    {
        public string SttModel { get; set; } = "gpt-4o-mini-transcribe";
        public string SttFallbackModel { get; set; } = "whisper-1";
        public string TtsModel { get; set; } = "gpt-4o-mini-tts";
        public string TtsVoice { get; set; } = "alloy";
        public float TtsSpeed { get; set; } = 1.0f;
        /// <summary>History user id used for the voice page (the endpoints skip the X-User-Id header).</summary>
        public string UserId { get; set; } = "voice-web";
    }

    public record TranscriptionResult(string Text, string? Language, string Model);

    public interface IVoiceService
    {
        Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, CancellationToken ct);
        Task<byte[]> SynthesizeMp3Async(string text, CancellationToken ct);
    }

    /// <summary>OpenAI speech-to-text (with fallback model) and text-to-speech (mp3).</summary>
    public class OpenAiVoiceService : IVoiceService
    {
        private const int MaxTtsChars = 4000; // API limit is 4096

        private readonly VoiceOptions _options;
        private readonly string _apiKey;
        private readonly ILogger _logger;

        public OpenAiVoiceService(IOptions<VoiceOptions> options, AppConfig appConfig, ILogger logger)
        {
            _options = options.Value;
            _apiKey = appConfig.OpenAIKey ?? throw new InvalidOperationException("OpenAI API key is not configured.");
            _logger = logger;
        }

        public async Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, CancellationToken ct)
        {
            // Buffer once so the fallback model can re-read the audio.
            using var buffer = new MemoryStream();
            await audio.CopyToAsync(buffer, ct);

            var models = new[] { _options.SttModel, _options.SttFallbackModel }
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .ToArray();

            for (var i = 0; i < models.Length; i++)
            {
                var model = models[i];
                try
                {
                    buffer.Position = 0;
                    var client = new AudioClient(model, _apiKey);
                    var options = new AudioTranscriptionOptions
                    {
                        // gpt-4o-*-transcribe only supports json/text; whisper-1 verbose_json also reports the language.
                        ResponseFormat = model.StartsWith("whisper", StringComparison.OrdinalIgnoreCase)
                            ? AudioTranscriptionFormat.Verbose
                            : AudioTranscriptionFormat.Simple
                    };
                    // No Language set -> the model detects it (Armenian, Russian, English...).
                    AudioTranscription result = await client.TranscribeAudioAsync(buffer, fileName, options, ct);
                    _logger.Information("STT {Model}: \"{Text}\" (lang {Lang})", model, result.Text, result.Language);
                    return new TranscriptionResult(result.Text?.Trim() ?? string.Empty, result.Language, model);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (i < models.Length - 1)
                {
                    _logger.Warning(ex, "STT with {Model} failed, falling back to {Fallback}", model, models[i + 1]);
                }
            }

            throw new InvalidOperationException("No speech-to-text model configured.");
        }

        public async Task<byte[]> SynthesizeMp3Async(string text, CancellationToken ct)
        {
            if (text.Length > MaxTtsChars) text = text[..MaxTtsChars];

            var client = new AudioClient(_options.TtsModel, _apiKey);
            var options = new SpeechGenerationOptions
            {
                ResponseFormat = GeneratedSpeechFormat.Mp3,
                SpeedRatio = _options.TtsSpeed
            };
            BinaryData speech = await client.GenerateSpeechAsync(text, new GeneratedSpeechVoice(_options.TtsVoice), options, ct);
            return speech.ToArray();
        }
    }
}
