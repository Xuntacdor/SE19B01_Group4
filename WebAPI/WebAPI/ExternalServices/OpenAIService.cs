using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebAPI.ExternalServices
{
    public class AiOptions
    {
        public string? Provider { get; set; }
        public string? BaseUrl { get; set; }
        public string? ChatModel { get; set; }
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<OpenAIService> _logger;
        private readonly string _chatModel;

        public OpenAIService(OpenAIClient client, ILogger<OpenAIService> logger, IOptions<AiOptions> aiOptions)
        {
            _client = client;
            _logger = logger;
            _chatModel = aiOptions.Value.ChatModel ?? "qwen2.5-7b-instruct-1m"; // default cho LM Studio
        }


        public JsonDocument GradeWriting(string question, string answer, string? imageUrl = null)
        {
            try
            {
                var chatClient = _client.GetChatClient(_chatModel);
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
                    new SystemChatMessage("You are a certified IELTS Writing examiner. Always return valid JSON following the schema exactly."),
                    new UserChatMessage(prompt)
                };

                var result = chatClient.CompleteChat(messages, new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 2500,
                    Temperature = 0.3f
                });

                string raw = result.Value.Content[0].Text ?? "{}";

                // Cắt phần JSON hợp lệ
                int first = raw.IndexOf('{');
                int last = raw.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first)
                    ? raw.Substring(first, last - first + 1)
                    : "{}";

                // Chuẩn hóa JSON
                jsonText = Regex.Replace(jsonText, @"\r\n|\r|\n", " ");
                jsonText = Regex.Replace(jsonText, @"\s+", " ").Trim();

                _logger.LogInformation("[OpenAIService] Writing JSON feedback generated successfully");

                return JsonDocument.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[OpenAIService] Failed to parse JSON from Writing output.");
                return JsonDocument.Parse(@"{ ""error"": ""Invalid JSON returned from AI"" }");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] Writing grading failed.");
                var msg = ex.Message.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
                return JsonDocument.Parse($@"{{ ""error"": ""{msg}"" }}");
            }
        }

        // ========================================
        // == 2.Speaking Suggestion ==
        // ========================================

        public JsonDocument GetSpeakingSuggestion(string question)
        {
            try
            {
                var chatClient = _client.GetChatClient(_chatModel);
                string prompt = $@"
You are an **IELTS Speaking coach** and English linguist.
Given an IELTS Speaking question, produce JSON that includes:

1️. A short, natural **sample answer** (Band 7–8 style).
2️. A list of **topic-related vocabulary** divided by level.

Return **STRICT JSON ONLY**, following this exact structure:

{{
  ""question"": ""original question"",
  ""sample_answer"": ""2–4 sentences model answer in natural IELTS English."",
  ""vocabulary_by_level"": {{
    ""basic"": [
      ""5–10 simple common words relevant to the topic""
    ],
    ""intermediate"": [
      ""5–10 moderately advanced words or short phrases""
    ],
    ""advanced"": [
      ""5–10 high-level or idiomatic expressions""
    ]
  }}
}}

IELTS Speaking Question:
{question}
";

                var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a certified IELTS Speaking coach. Always return valid JSON following the schema."),
            new UserChatMessage(prompt)
        };

                var result = chatClient.CompleteChat(messages, new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 1000,
                    Temperature = 0.7f
                });

                string raw = result.Value.Content[0].Text ?? "{}";
                int first = raw.IndexOf('{');
                int last = raw.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first)
                    ? raw.Substring(first, last - first + 1)
                    : "{}";

                jsonText = Regex.Replace(jsonText, @"\r\n|\r|\n", " ");
                jsonText = Regex.Replace(jsonText, @"\s+", " ").Trim();

                _logger.LogInformation("[OpenAIService] Speaking suggestion JSON generated successfully");

                return JsonDocument.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from Speaking suggestion output.");
                return JsonDocument.Parse(@"{ ""error"": ""Invalid JSON returned from AI"" }");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] Speaking suggestion generation failed.");
                var msg = ex.Message.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
                return JsonDocument.Parse($@"{{ ""error"": ""{msg}"" }}");
            }
        }

        // ========================================
        // == 3. SPEAKING GRADER ==
        // ========================================
        public JsonDocument GradeSpeaking(string question, string transcript)
        {
            try
            {
                var chatClient = _client.GetChatClient(_chatModel);

                string prompt = $@"
You are an **IELTS Speaking examiner**.
Evaluate the candidate's speaking based on the transcript below.
Use ONLY the four official IELTS Speaking criteria:

1. Fluency & Coherence  
2. Lexical Resource  
3. Grammatical Range & Accuracy  
4. Pronunciation

 IMPORTANT:
- Do NOT generate an overall score.
- Do NOT generate a separate 'coherence' score (IELTS combines it with fluency).
- Scores must be integers or half bands (e.g., 5, 5.5, 6, 6.5, 7).

Return **STRICT JSON ONLY**, following this schema exactly:

{{
  ""band_estimate"": {{
    ""pronunciation"": 0–9,
    ""fluency"": 0–9,
    ""lexical_resource"": 0–9,
    ""grammar_accuracy"": 0–9
  }},
  ""ai_analysis"": {{
    ""overview"": ""3–5 sentence summary of performance."",
    ""strengths"": [""list strengths""],
    ""weaknesses"": [""list weaknesses""],
    ""advice"": ""1–2 sentences of actionable improvement tips."",
    ""vocabulary_suggestions"": [
      {{
        ""original_word"": ""example word or phrase used by the candidate"",
        ""suggested_alternative"": ""better alternative"",
        ""explanation"": ""why it is better""
      }}
    ]
  }}
}}

Question:
{question}

Transcript:
{transcript}
";

                var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a certified IELTS Speaking examiner. Always return strictly valid JSON following the schema."),
            new UserChatMessage(prompt)
        };

                var result = chatClient.CompleteChat(messages, new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 1800,
                    Temperature = 0.4f
                });

                string raw = result.Value.Content[0].Text ?? "{}";

                // Extract clean JSON
                int first = raw.IndexOf('{');
                int last = raw.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first)
                    ? raw.Substring(first, last - first + 1)
                    : "{}";

                jsonText = Regex.Replace(jsonText, @"\r\n|\r|\n", " ");
                jsonText = Regex.Replace(jsonText, @"\s+", " ").Trim();

                _logger.LogInformation("[OpenAIService] IELTS Speaking JSON feedback generated successfully");

                return JsonDocument.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from Speaking output.");
                return JsonDocument.Parse(@"{ ""error"": ""Invalid JSON returned from AI"" }");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] Speaking grading failed.");
                var msg = ex.Message.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
                return JsonDocument.Parse($@"{{ ""error"": ""{msg}"" }}");
            }
        }

        public JsonDocument LookupWordAI(string query)
        {
            try
            {
                var chatClient = _client.GetChatClient(_chatModel);
                string prompt = $@"
You are a bilingual English–Vietnamese dictionary assistant.

Detect the input language automatically and RETURN STRICT JSON ONLY with EXACT keys below:

{{
  ""term"": ""original user input"",
  ""detected_language"": ""English"" | ""Vietnamese"",
  ""englishTranslation"": ""natural English translation of the term (if input is Vietnamese). If input is English, repeat the original term or leave an empty string."",
  ""vietnameseTranslation"": ""tự nhiên, súc tích tiếng Việt (nếu input là English). Nếu input là Vietnamese thì để chuỗi rỗng."",
  ""example"": ""one natural English sentence using the term (or its translation)""
}}

Keep it concise. Do not include extra commentary or markdown.

Input: {query}
";

                var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You return only valid JSON with the exact keys requested."),
            new UserChatMessage(prompt)
        };

                var result = chatClient.CompleteChat(messages, new ChatCompletionOptions
                {
                    Temperature = 0.3f,
                    MaxOutputTokenCount = 800
                });

                string raw = result.Value.Content[0].Text ?? "{}";
                int first = raw.IndexOf('{');
                int last = raw.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first) ? raw.Substring(first, last - first + 1) : "{}";
                jsonText = Regex.Replace(jsonText, @"\s+", " ").Trim();

                return JsonDocument.Parse(jsonText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] LookupWordAI failed.");
                var msg = ex.Message.Replace("\"", "\\\"");
                return JsonDocument.Parse($@"{{ ""error"": ""{msg}"" }}");
            }
        }

    }
}
