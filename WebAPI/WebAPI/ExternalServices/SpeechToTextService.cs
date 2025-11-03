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
    public class SpeechToTextService : ISpeechToTextService
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

  
        public string TranscribeAndSave(long attemptId, string audioUrl)
        {
            if (string.IsNullOrWhiteSpace(audioUrl))
                throw new ArgumentException("Audio URL is required.");

            try
            {
                _logger.LogInformation("[SpeechToText] Downloading audio: {Url}", audioUrl);

                // 1) Download audio file from Cloudinary
                _logger.LogInformation("[SpeechToText] Attempting to download from Cloudinary...");
                using var response = _http.GetAsync(audioUrl).Result;
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[SpeechToText] Failed to download audio. Status: {Status}, Reason: {Reason}", 
                        response.StatusCode, response.ReasonPhrase);
                    return "[Transcription failed - Audio download failed]";
                }
                
                _logger.LogInformation("[SpeechToText] Audio downloaded successfully, size: {Size} bytes", 
                    response.Content.Headers.ContentLength ?? 0);
                
                using var audioStream = response.Content.ReadAsStreamAsync().Result;
                
                // Determine file format from URL or content
                string fileName = GetAudioFileName(audioUrl);
                string mimeType = GetMimeTypeFromFileName(fileName);
                
                _logger.LogInformation("[SpeechToText] Detected file: {FileName}, MIME: {MimeType}", fileName, mimeType);
                
                // Log the final file format being sent to Whisper
                _logger.LogInformation("[SpeechToText] Sending to Whisper with file: {FileName}, MIME: {MimeType}", fileName, mimeType);
                
                using var audioContent = new StreamContent(audioStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

                // 2) Build multipart form for Whisper
                using var form = new MultipartFormDataContent();
                form.Add(audioContent, "file", fileName);                     // Use proper filename with extension
                form.Add(new StringContent("whisper-1"), "model");            // model
                form.Add(new StringContent("json"), "response_format");       // json
                // default to English
                form.Add(new StringContent("en"), "language");            // "en" or your choice

                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                req.Content = form;

                _logger.LogInformation("[SpeechToText] Sending to Whisper...");
                using var resp = _http.Send(req);
                var body = resp.Content.ReadAsStringAsync().Result;

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("[SpeechToText] Whisper error: {Status} {Body}", (int)resp.StatusCode, body);
                    
                    // Parse error details for better logging
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(body);
                        if (errorDoc.RootElement.TryGetProperty("error", out var errorEl))
                        {
                            var message = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                            var type = errorEl.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "Unknown type";
                            _logger.LogError("[SpeechToText] Whisper API error - Type: {Type}, Message: {Message}", type, message);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "[SpeechToText] Failed to parse Whisper error response");
                    }
                    
                    return "[Transcription failed]";
                }

                // 3) Parse { "text": "..." }
                using var doc = JsonDocument.Parse(body);
                var transcript = doc.RootElement.TryGetProperty("text", out var textEl)
                    ? textEl.GetString() ?? string.Empty
                    : string.Empty;

                _logger.LogInformation("[SpeechToText] Transcription OK: {Transcript}", transcript);

                // 4) Try to save to ExamAttempt.AnswerText as JSON { audioUrl, transcript }
                
                return transcript;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeechToText] Failed to transcribe.");
                return "[Transcription failed]";
            }
        }

        /// <summary>
        /// Extract filename from Cloudinary URL, defaulting to webm if no extension found
        /// </summary>
        private string GetAudioFileName(string audioUrl)
        {
            try
            {
                var uri = new Uri(audioUrl);
                var fileName = Path.GetFileName(uri.LocalPath);
                
                // If no extension, default to webm (most common format from MediaRecorder)
                if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                {
                    fileName = "speech_audio.webm";
                }
                // If unsupported extension, change to webm
                else if (!IsSupportedAudioFormat(Path.GetExtension(fileName).ToLowerInvariant()))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    fileName = $"{nameWithoutExt}.webm";
                }
                
                return fileName;
            }
            catch
            {
                return "speech_audio.webm";
            }
        }

        /// <summary>
        /// Get MIME type based on file extension
        /// </summary>
        private string GetMimeTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".webm" => "audio/webm",
                ".mp3" => "audio/mpeg",
                ".mp4" => "audio/mp4",
                ".m4a" => "audio/mp4",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".oga" => "audio/ogg",
                ".flac" => "audio/flac",
                ".mpga" => "audio/mpeg",
                _ => "audio/webm" // Default to webm
            };
        }

        /// <summary>
        /// Check if the file extension is supported by Whisper
        /// </summary>
        private bool IsSupportedAudioFormat(string extension)
        {
            var supportedFormats = new[] { ".webm", ".mp3", ".mp4", ".m4a", ".wav", ".ogg", ".oga", ".flac", ".mpga" };
            return supportedFormats.Contains(extension);
        }

        /// <summary>
        /// Test if Cloudinary URL is accessible
        /// </summary>
        public bool TestCloudinaryAccess(string audioUrl)
        {
            try
            {
                _logger.LogInformation("[SpeechToText] Testing Cloudinary access: {Url}", audioUrl);
                using var response = _http.GetAsync(audioUrl).Result;
                var isAccessible = response.IsSuccessStatusCode;
                _logger.LogInformation("[SpeechToText] Cloudinary access test result: {IsAccessible}, Status: {Status}", 
                    isAccessible, response.StatusCode);
                return isAccessible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeechToText] Cloudinary access test failed");
                return false;
            }
        }

        /// <summary>
        /// Transcribe audio from local file path
        /// </summary>
        public string TranscribeFromFile(string filePath, long attemptId, string language = "en")
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new ArgumentException("File path is invalid or file does not exist.");

            try
            {
                _logger.LogInformation("[SpeechToText] Transcribing from file: {FilePath}", filePath);

                // Read file
                var audioBytes = File.ReadAllBytes(filePath);
                var fileName = Path.GetFileName(filePath);
                var mimeType = GetMimeTypeFromFileName(fileName);

                _logger.LogInformation("[SpeechToText] File loaded: {FileName}, Size: {Size} bytes, MIME: {MimeType}", 
                    fileName, audioBytes.Length, mimeType);

                using var audioStream = new MemoryStream(audioBytes);
                using var audioContent = new StreamContent(audioStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

                using var form = new MultipartFormDataContent();
                form.Add(audioContent, "file", fileName);
                form.Add(new StringContent("whisper-1"), "model");
                form.Add(new StringContent("json"), "response_format");
                form.Add(new StringContent(language), "language");

                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                req.Content = form;

                _logger.LogInformation("[SpeechToText] Sending to Whisper...");
                using var resp = _http.Send(req);
                var body = resp.Content.ReadAsStringAsync().Result;

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("[SpeechToText] Whisper error: {Status} {Body}", (int)resp.StatusCode, body);
                    
                    // Parse error details for better logging
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(body);
                        if (errorDoc.RootElement.TryGetProperty("error", out var errorEl))
                        {
                            var message = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                            var type = errorEl.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "Unknown type";
                            _logger.LogError("[SpeechToText] Whisper API error - Type: {Type}, Message: {Message}", type, message);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "[SpeechToText] Failed to parse Whisper error response");
                    }
                    
                    return "[Transcription failed]";
                }

                using var doc = JsonDocument.Parse(body);
                var transcript = doc.RootElement.TryGetProperty("text", out var textEl)
                    ? textEl.GetString() ?? string.Empty
                    : string.Empty;

                _logger.LogInformation("[SpeechToText] Transcription OK: {Transcript}", transcript);

                // Save to database if needed
                try
                {
                    var attempt = _examService.GetAttemptById(attemptId);
                    if (attempt != null)
                    {
                        var payloadJson = JsonSerializer.Serialize(new
                        {
                            audioUrl = filePath,
                            transcript
                        });

                        attempt.AnswerText = payloadJson;
                        _examService.Save();
                        _logger.LogInformation("[SpeechToText] Saved transcript into ExamAttempt.AnswerText.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SpeechToText] Failed to save transcript to database, but transcription was successful.");
                }

                return transcript;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeechToText] Failed to transcribe from file.");
                return "[Transcription failed]";
            }
        }
    }
}
