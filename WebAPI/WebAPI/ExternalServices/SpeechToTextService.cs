using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WebAPI.Services;

namespace WebAPI.Services
{
    public class SpeechToTextService
    {
        private readonly string _apiKey;
        private readonly ILogger<SpeechToTextService> _logger;
        private readonly HttpClient _http;
        private readonly IExamService _examService;

        public SpeechToTextService(IConfiguration config, ILogger<SpeechToTextService> logger, IExamService examService)
        {
            _apiKey = config["OpenAI:ApiKey"] ?? throw new ArgumentException("Missing OpenAI API key.");
            _logger = logger;
            _http = new HttpClient();
            _examService = examService;
        }

        /// <summary>
        /// Download Cloudinary audio → Whisper (REST) → transcript → save to ExamAttempt.AnswerText (JSON).
        /// </summary>
        public string TranscribeAndSave(long attemptId, string audioUrl, string language = "en")
        {
            if (string.IsNullOrWhiteSpace(audioUrl))
                throw new ArgumentException("Audio URL is required.");

            try
            {
                _logger.LogInformation("[SpeechToText] Downloading audio: {Url}", audioUrl);

                // 1) Download audio file from Cloudinary
                using var audioStream = _http.GetStreamAsync(audioUrl).Result;
                using var audioContent = new StreamContent(audioStream);
                // Content-Type của file audio để trống cũng được; nếu biết rõ có thể set "audio/mpeg" hoặc "audio/wav"

                // 2) Build multipart form for Whisper
                using var form = new MultipartFormDataContent();
                form.Add(audioContent, "file", "speech_audio");               // name must be "file"
                form.Add(new StringContent("whisper-1"), "model");            // model
                form.Add(new StringContent("json"), "response_format");       // json
                form.Add(new StringContent(language), "language");            // "en" or your choice

                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                req.Content = form;

                _logger.LogInformation("[SpeechToText] Sending to Whisper...");
                using var resp = _http.Send(req);
                var body = resp.Content.ReadAsStringAsync().Result;

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("[SpeechToText] Whisper error: {Status} {Body}", (int)resp.StatusCode, body);
                    return "[Transcription failed]";
                }

                // 3) Parse { "text": "..." }
                using var doc = JsonDocument.Parse(body);
                var transcript = doc.RootElement.TryGetProperty("text", out var textEl)
                    ? textEl.GetString() ?? string.Empty
                    : string.Empty;

                _logger.LogInformation("[SpeechToText] Transcription OK: {Transcript}", transcript);

                // 4) Save to ExamAttempt.AnswerText as JSON { audioUrl, transcript }
                var attempt = _examService.GetAttemptById(attemptId) ?? throw new Exception($"ExamAttempt not found (ID={attemptId})");

                var payloadJson = JsonSerializer.Serialize(new
                {
                    audioUrl,
                    transcript
                });

                attempt.AnswerText = payloadJson;
                _examService.Save();

                _logger.LogInformation("[SpeechToText] Saved transcript into ExamAttempt.AnswerText.");
                return transcript;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeechToText] Failed to transcribe.");
                return "[Transcription failed]";
            }
        }
    }
}
