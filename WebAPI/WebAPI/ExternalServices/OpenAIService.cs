using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Audio;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
namespace WebAPI.ExternalServices
{
    public class OpenAIService
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIService(IConfiguration config, ILogger<OpenAIService> logger)
        {
            var apiKey = config["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("OpenAI API key is not configured.");

            _client = new OpenAIClient(apiKey);
            _logger = logger;
        }

        // ========================================
        // == 1. WRITING GRADER ==
        // ========================================
        public JsonDocument GradeWriting(string question, string answer, string? imageUrl = null)
        {
            try
            {
                var chatClient = _client.GetChatClient("gpt-4o");
                string? base64 = null;
                string mimeType = "image/png";

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    try
                    {
                        var (b64, mime) = ImageConverter.GetBase64FromUrl(imageUrl);
                        base64 = b64;
                        mimeType = mime;
                        _logger.LogInformation("[OpenAIService] Image converted successfully ({MimeType})", mimeType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[OpenAIService] Failed to convert image {Url}", imageUrl);
                    }
                }

                string prompt = $@"
You are an **IELTS Writing examiner**.
Grade the student's essay using official IELTS Writing band descriptors:
Task Achievement, Coherence & Cohesion, Lexical Resource, Grammatical Range & Accuracy.

Return **strict JSON only**, following this structure:

{{
  ""grammar_vocab"": {{
    ""overview"": ""3–5 sentences"",
    ""errors"": [{{ ""type"": ""Grammar"", ""category"": ""Tense"", ""incorrect"": ""..."", ""suggestion"": ""..."", ""explanation"": ""..."" }}]
  }},
  ""coherence_logic"": {{
    ""overview"": ""summary of coherence and logic"",
    ""paragraph_feedback"": [{{ ""section"": ""Body 1"", ""strengths"": [], ""weaknesses"": [], ""advice"": ""..."" }}]
  }},
  ""band_estimate"": {{
    ""task_achievement"": 0–9,
    ""coherence_cohesion"": 0–9,
    ""lexical_resource"": 0–9,
    ""grammar_accuracy"": 0–9,
    ""overall"": 0–9
  }}
}}

Essay Question:
{question}

Essay Answer:
{answer}
";

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a certified IELTS Writing examiner. Always return valid JSON following the schema exactly.")
                };

                if (!string.IsNullOrEmpty(base64))
                {
                    messages.Add(ChatMessage.CreateUserMessage(
                        ChatMessageContentPart.CreateTextPart($"Analyze the image and essay below:\n{prompt}"),
                        ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(Convert.FromBase64String(base64)), mimeType)
                    ));
                }
                else
                {
                    messages.Add(new UserChatMessage(prompt));
                }

                var result = chatClient.CompleteChat(messages, new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 2500,
                    Temperature = 0.3f
                });

                var raw = result.Value.Content[0].Text ?? "{}";
                int first = raw.IndexOf('{');
                int last = raw.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first)
                    ? raw.Substring(first, last - first + 1)
                    : "{}";

                _logger.LogInformation("[OpenAIService] Writing JSON feedback generated successfully:\n{Json}", jsonText);
                return JsonDocument.Parse(jsonText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] Writing grading failed.");
                return JsonDocument.Parse($@"{{ ""error"": ""{ex.Message}"" }}");
            }
        }

        // ========================================
        // == 2. SPEECH-TO-TEXT ==
        // ========================================
        public string SpeechToText(string audioUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(audioUrl))
                    throw new ArgumentException("Audio URL is empty.");

                var audioClient = _client.GetAudioClient("gpt-4o-mini-tts"); // or whisper-1 if available
                _logger.LogInformation("[OpenAIService] Starting transcription for {AudioUrl}", audioUrl);

                // In production, download Cloudinary file to memory stream before sending.
                // Here we mock transcript for development.
                return "This is a sample transcript from the uploaded audio.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] Speech-to-text failed.");
                return "[Transcription failed]";
            }
        }

        // ========================================
        // == 3. SPEAKING GRADER ==
        // ========================================
        public JsonDocument GradeSpeaking(string question, string transcript)
        {
            try
            {
                var chatClient = _client.GetChatClient("gpt-4o");

                string prompt = $@"
You are an **IELTS Speaking examiner**.
Evaluate the candidate's speaking based on the transcript below.
Use IELTS Speaking band descriptors (Fluency & Coherence, Lexical Resource, Grammar Accuracy, Pronunciation).
Return **strict JSON only**, no markdown or commentary.

{{
  ""band_estimate"": {{
    ""pronunciation"": 0–9,
    ""fluency"": 0–9,
    ""lexical_resource"": 0–9,
    ""grammar_accuracy"": 0–9,
    ""coherence"": 0–9,
    ""overall"": 0–9
  }},
  ""ai_analysis"": {{
    ""overview"": ""3–5 sentences summarizing performance."",
    ""strengths"": [""clear pronunciation"", ""good coherence""],
    ""weaknesses"": [""occasional pauses"", ""limited vocabulary""],
    ""advice"": ""specific tips to improve""
  }}
}}

Transcript:
{transcript}
";

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a certified IELTS Speaking examiner. Always return valid JSON following the schema."),
                    new UserChatMessage(prompt)
                };

                var result = chatClient.CompleteChat(messages, new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 1800,
                    Temperature = 0.4f
                });

                var raw = result.Value.Content[0].Text ?? "{}";
                int first = raw.IndexOf('{');
                int last = raw.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first)
                    ? raw.Substring(first, last - first + 1)
                    : "{}";

                _logger.LogInformation("[OpenAIService] Speaking JSON feedback generated successfully:\n{Json}", jsonText);
                return JsonDocument.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from Speaking output.");
                return JsonDocument.Parse(@"{ ""error"": ""Invalid JSON returned from OpenAI"" }");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] Speaking grading failed.");
                return JsonDocument.Parse($@"{{ ""error"": ""{ex.Message}"" }}");
            }
        }
    }
}
