using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
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

        /// <summary>
        /// Grade an IELTS Writing Task (Task 1 or Task 2) and return structured JSON feedback.
        /// Supports image-based Task 1 via base64 image embedding.
        /// </summary>
        public JsonDocument GradeWriting(string question, string answer, string? imageUrl = null)
        {
            try
            {
                // ✅ Model tốt nhất cho bài viết + ảnh + JSON ổn định
                var chatClient = _client.GetChatClient("gpt-4o");

                // === Step 1. Convert image (if exists)
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

                // === Step 2. Build the detailed grading prompt
                string prompt = $@"
You are an **IELTS Writing examiner**.
You must grade the student's essay following the **official IELTS Writing band descriptors**:
Task Achievement, Coherence & Cohesion, Lexical Resource, and Grammatical Range & Accuracy.

Your output must be **pure JSON**, following this structure EXACTLY — no markdown, no explanation outside JSON.

{{
  ""grammar_vocab"": {{
    ""overview"": ""3–5 sentences summarizing grammar and vocabulary performance."",
    ""errors"": [
      {{
        ""type"": ""Grammar"" or ""Vocabulary"",
        ""category"": ""e.g. Tense, Word Choice, Collocation, Article, Preposition, Word Form, Lexical Range"",
        ""incorrect"": ""the student's incorrect phrase"",
        ""suggestion"": ""corrected phrase"",
        ""explanation"": ""brief reason why it’s wrong (≤2 sentences)""
      }}
    ]
  }},
  ""coherence_logic"": {{
    ""overview"": ""3–4 sentences summarizing task achievement, coherence, cohesion, and logical development."",
    ""paragraph_feedback"": [
      {{
        ""section"": ""Introduction"",
        ""strengths"": [""clear overview"", ""effective paraphrasing""],
        ""weaknesses"": [""missing thesis"", ""limited coherence""],
        ""advice"": ""Specific suggestion to improve introduction clarity.""
      }},
      {{
        ""section"": ""Body 1"",
        ""strengths"": [""logical argument"", ""appropriate examples""],
        ""weaknesses"": [""unclear topic sentence"", ""repetitive linking words""],
        ""advice"": ""Concrete suggestion for improving argumentation.""
      }},
      {{
        ""section"": ""Body 2"",
        ""strengths"": [""good cohesion"", ""clear comparison""],
        ""weaknesses"": [""unsupported claims"", ""limited lexical variety""],
        ""advice"": ""How to make analysis more persuasive.""
      }},
      {{
        ""section"": ""Overview"",
        ""strengths"": [""clear summary of trends""],
        ""weaknesses"": [""omits key features""],
        ""advice"": ""Advice on improving summary and conclusion.""
      }}
    ]
  }},
  ""band_estimate"": {{
    ""task_achievement"": 0–9,
    ""coherence_cohesion"": 0–9,
    ""lexical_resource"": 0–9,
    ""grammar_accuracy"": 0–9,
    ""overall"": 0–9
  }}
}}

### Instructions:
- Return **strict JSON** (no markdown).
- Include **6–10 Grammar/Vocabulary errors** with clear explanations.
- Each paragraph must contain both strengths and weaknesses.
- Use real examples from the essay text wherever possible.
- Keep tone professional, concise, and examiner-like.

Now evaluate this essay:

Essay Question:
{question}

Essay Answer:
{answer}
";

                // === Step 3. Create Chat messages
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a certified IELTS Writing examiner. Always return valid JSON that strictly follows the schema above.")
                };

                if (!string.IsNullOrEmpty(base64))
                {
                    messages.Add(ChatMessage.CreateUserMessage(
                        ChatMessageContentPart.CreateTextPart($"IELTS Writing Task 1:\n{question}\nAnalyze the image and essay below."),
                        ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(Convert.FromBase64String(base64)), mimeType),
                        ChatMessageContentPart.CreateTextPart($"Essay:\n{answer}\n\n{prompt}")
                    ));
                }
                else
                {
                    messages.Add(new UserChatMessage(prompt));
                }

                // === Step 4. Call OpenAI (extended tokens, low randomness)
                var result = chatClient.CompleteChat(
                    messages,
                    new ChatCompletionOptions
                    {
                        MaxOutputTokenCount = 2500,
                        Temperature = 0.3f
                    }
                );

                var raw = result.Value.Content[0].Text ?? "{}";

                // === Step 5. Extract JSON only
                int first = raw.IndexOf('{');
                int last = raw.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first)
                    ? raw.Substring(first, last - first + 1)
                    : "{}";

                _logger.LogInformation("[OpenAIService] JSON feedback generated successfully.");
                _logger.LogInformation("[OpenAIService] Raw JSON Output:\n{JsonText}", jsonText);

                // === Step 6. Parse and return
                return JsonDocument.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from OpenAI output.");
                return JsonDocument.Parse(@"{ ""error"": ""Invalid JSON returned from OpenAI"" }");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI call failed.");
                return JsonDocument.Parse($@"{{ ""error"": ""{ex.Message}"" }}");
            }
        }
    }
}
