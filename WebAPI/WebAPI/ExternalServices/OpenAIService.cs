using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Audio;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebAPI.ExternalServices
{
    public class OpenAIService : IOpenAIService
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIService(OpenAIClient client, ILogger<OpenAIService> logger)
        {
            _client = client;
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
You are an **IELTS Writing examiner** with years of experience in scoring IELTS essays.
Evaluate the following essay in detail based on the official IELTS Writing Task 2 descriptors.

Provide feedback focusing on:
- Grammar and vocabulary issues (detailed correction + explanation)
- Logic, coherence, and cohesion
- Overall impression and improvement advice
- Refined word/phrase suggestions (show before → after + explanation)

Return **STRICT JSON ONLY**, matching this structure exactly:

{{
  ""grammar_vocab"": {{
    ""overview"": ""2–4 sentences summarizing grammar and vocabulary quality."",
    ""errors"": [
      {{
        ""type"": ""Grammar"" | ""Vocabulary"",
        ""category"": ""e.g. Tense"", 
        ""incorrect"": ""original sentence or phrase"",
        ""suggestion"": ""corrected version"",
        ""explanation"": ""why it is wrong and how to fix""
      }}
    ]
  }},
  
  ""overall_feedback"": {{
    ""overview"": ""3–5 sentences summarizing coherence, logic, idea development, and task achievement."",
    ""refinements"": [
      {{
        ""original"": ""weak or overused word/phrase"",
        ""improved"": ""more natural academic alternative"",
        ""explanation"": ""why the new choice is better in lexical accuracy or collocation.""
      }}
    ]
  }},
  
  ""band_estimate"": {{
    ""task_achievement"": 0–9,
    ""organization_logic"": 0–9,
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

                // FIX: Remove all newlines and normalize whitespace
                jsonText = Regex.Replace(jsonText, @"\r\n|\r|\n", " ");
                jsonText = Regex.Replace(jsonText, @"\s+", " ").Trim();

                _logger.LogInformation("[OpenAIService] Writing JSON feedback generated successfully");

                return JsonDocument.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[OpenAIService] Failed to parse JSON from Writing output.");
                return JsonDocument.Parse(@"{ ""error"": ""Invalid JSON returned from OpenAI"" }");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] Writing grading failed.");
                var errorMessage = ex.Message.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
                return JsonDocument.Parse($@"{{ ""error"": ""{errorMessage}"" }}");
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
    ""advice"": ""Provide 1-2 specific tips to improve overall speaking score."",
    ""vocabulary_suggestions"": [
        {{
            ""original_word"": ""The word or phrase used by the candidate"",
            ""suggested_alternative"": ""A better, more precise, or less common alternative"",
            ""explanation"": ""Explain why the alternative is better (e.g., more idiomatic, less common, more precise context).""
        }}
    ]
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

                // FIX: Remove all newlines and normalize whitespace
                jsonText = Regex.Replace(jsonText, @"\r\n|\r|\n", " ");
                jsonText = Regex.Replace(jsonText, @"\s+", " ").Trim();

                _logger.LogInformation("[OpenAIService] Speaking JSON feedback generated successfully");

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
                var errorMessage = ex.Message.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
                return JsonDocument.Parse($@"{{ ""error"": ""{errorMessage}"" }}");
            }
        }
    }
}