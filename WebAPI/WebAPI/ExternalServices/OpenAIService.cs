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
            _chatModel = aiOptions.Value.ChatModel ?? "gpt-4o-mini"; // default cho Cloud OpenAI API
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
You are an IELTS Speaking coach and linguist.
Given an IELTS Speaking question, return STRICT JSON ONLY using this exact schema:

{{
  """"question"""": """"original question"""",
  """"sample_answer"""": """"2–4 sentences model answer in natural IELTS English."""",
  """"vocabulary_by_level"""": {{
    """"basic"""": [
      {{
        """"term"""": """"word"""",
        """"vn"""": """"nghĩa tiếng Việt""""
      }}
    ],
    """"intermediate"""": [
      {{
        """"term"""": """"word"""",
        """"vn"""": """"nghĩa tiếng Việt""""
      }}
    ],
    """"advanced"""": [
      {{
        """"term"""": """"word"""",
        """"vn"""": """"nghĩa tiếng Việt""""
      }}
    ]
  }}
}}

IMPORTANT RULES:
- DO NOT change key names.
- For each level (basic/intermediate/advanced), return 5–10 items.
- Each item must be an object: {{ """"term"""": """"..."""", """"vn"""": """"..."""" }}.
- """"term"""" must be an English word/phrase.
- """"vn"""" must be the correct Vietnamese meaning.
- DO NOT return any comments or markdown.

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

Your ONLY response must be a single JSON object with EXACTLY these keys:

{{
  ""term"": ""original user input"",
  ""detected_language"": ""English"" | ""Vietnamese"",
  ""englishTranslation"": ""ALWAYS NON-EMPTY. 
    If input is English: paraphrase or define meaning in natural English. 
    If input is Vietnamese: translate to natural English."",
  ""vietnameseTranslation"": ""ALWAYS NON-EMPTY.
    If input is English: translate to natural Vietnamese.
    If input is Vietnamese: paraphrase the meaning in natural Vietnamese."",
  ""example"": ""one natural English sentence using the English meaning""
}}

Strict rules:
- ALL fields MUST have non-empty string values.
- NEVER return empty strings.
- NEVER return null.
- NEVER add extra keys.
- NO markdown, NO explanations — ONLY the JSON object.

Input: ""{query}""
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

        // ========================================
        // == 5. CONTENT MODERATION ANALYSIS ==
        // ========================================
        public JsonDocument AnalyzeContent(string title, string content, string contentType = "post")
        {
            try
            {
                var chatClient = _client.GetChatClient(_chatModel);
                
                string prompt = $@"You are a content moderation assistant for a forum platform.
Analyze the following {(contentType == "post" ? "post" : "comment")} content and provide:

1. **Meaningfulness Check**: Determine if the content has meaningful value (not spam, gibberish, or meaningless text)
   - Check if the content conveys a clear message, idea, or information
   - Check if it's relevant and contributes to discussion
   - Check if it's spam, random characters, or meaningless content

2. **Bilingual Summary**: Provide a summary in BOTH English and Vietnamese
   - English summary: 2-3 sentences summarizing the main points
   - Vietnamese summary: 2-3 sentences summarizing the main points in Vietnamese

3. **Inappropriate Content Detection**: Identify any inappropriate words/phrases (profanity, hate speech, offensive language)
   - For each inappropriate word/phrase, provide:
     - The exact text
     - Start and end character positions (0-based index)
     - Type of inappropriate content (profanity, hate_speech, offensive, spam)
     - Brief explanation (optional)

Return **STRICT JSON ONLY**, following this exact structure:

{{
  ""is_meaningful"": true/false,
  ""meaningfulness_reason"": ""brief explanation why content is or isn't meaningful"",
  ""summary"": {{
    ""english"": ""2-3 sentence summary in English"",
    ""vietnamese"": ""2-3 câu tóm tắt bằng tiếng Việt""
  }},
  ""has_inappropriate_content"": true/false,
  ""inappropriate_words"": [
    {{
      ""text"": ""exact inappropriate word or phrase"",
      ""start_index"": 0,
      ""end_index"": 5,
      ""type"": ""profanity"" | ""hate_speech"" | ""offensive"" | ""spam"",
      ""explanation"": ""brief explanation (optional)""
    }}
  ]
}}

{(contentType == "post" ? $"Post Title:\n{title}\n\nPost Content:" : "Comment Content:")}
{content}";

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a content moderation assistant. Always return valid JSON following the schema exactly."),
                    new UserChatMessage(prompt)
                };

                var result = chatClient.CompleteChat(messages, new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 2000,
                    Temperature = 0.2f
                });

                string raw = result.Value.Content[0].Text ?? "{}";
                _logger.LogInformation("[OpenAIService] Raw AI response: {Raw}", raw);
                
                // Extract clean JSON
                int first = raw.IndexOf('{');
                int last = raw.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first)
                    ? raw.Substring(first, last - first + 1)
                    : "{}";

                jsonText = Regex.Replace(jsonText, @"\r\n|\r|\n", " ");
                jsonText = Regex.Replace(jsonText, @"\s+", " ").Trim();

                _logger.LogInformation("[OpenAIService] Cleaned JSON text: {JsonText}", jsonText);

                try
                {
                    var jsonDoc = JsonDocument.Parse(jsonText);
                    _logger.LogInformation("[OpenAIService] Content analysis JSON generated successfully");
                    return jsonDoc;
                }
                catch (JsonException parseEx)
                {
                    _logger.LogError(parseEx, "[OpenAIService] JSON parsing failed. JSON text: {JsonText}", jsonText);
                    throw;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[OpenAIService] Failed to parse JSON from content analysis output.");
                return JsonDocument.Parse(@"{ ""error"": ""Invalid JSON returned from AI"", ""is_meaningful"": true, ""meaningfulness_reason"": """", ""summary"": { ""english"": """", ""vietnamese"": """" }, ""has_inappropriate_content"": false, ""inappropriate_words"": [] }");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAIService] Content analysis failed.");
                var msg = ex.Message.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
                return JsonDocument.Parse($@"{{ ""error"": ""{msg}"", ""is_meaningful"": true, ""meaningfulness_reason"": """", ""summary"": {{ ""english"": """", ""vietnamese"": """" }}, ""has_inappropriate_content"": false, ""inappropriate_words"": [] }}");
            }
        }

    }
}
