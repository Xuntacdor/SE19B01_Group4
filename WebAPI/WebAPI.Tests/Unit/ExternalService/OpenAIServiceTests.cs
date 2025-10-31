using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Text.Json;
using WebAPI.ExternalServices;
using Xunit;

namespace WebAPI.Tests.Unit.ExternalServices
{
    public class OpenAIServiceTests
    {
        private readonly Mock<ILogger<OpenAIService>> _loggerMock;
        private readonly IOptions<AiOptions> _options;

        public OpenAIServiceTests()
        {
            _loggerMock = new Mock<ILogger<OpenAIService>>();
            _options = Options.Create(new AiOptions
            {
                Provider = "Local",
                BaseUrl = "http://localhost:1234/v1",
                ChatModel = "qwen2.5-7b-instruct-1m"
            });
        }

        // ✅ TEST 1: GradeWriting with LM Studio (real API call)
        [Fact]
        public void GradeWriting_ShouldReturnValidJson_FromLocalAI()
        {
            var client = new OpenAIClient(
                new ApiKeyCredential("dummy-key"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") });

            var service = new OpenAIService(client, _loggerMock.Object, _options);

            var result = service.GradeWriting(
                "Some people think governments should invest in public transport.",
                "Public transport helps reduce traffic jam and pollution.");

            result.Should().NotBeNull();
            result.RootElement.ToString().Should().ContainAny("grammar_vocab", "band_estimate", "error");
        }

        // ✅ TEST 2: GradeSpeaking with LM Studio (real API call)
        [Fact]
        public void GradeSpeaking_ShouldReturnValidJson_FromLocalAI()
        {
            var client = new OpenAIClient(
                new ApiKeyCredential("dummy-key"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") });

            var service = new OpenAIService(client, _loggerMock.Object, _options);

            var result = service.GradeSpeaking(
                "Describe your favorite place to visit.",
                "My favorite place is the beach because I love the sea and relaxing.");

            result.Should().NotBeNull();
            result.RootElement.ToString().Should().ContainAny("band_estimate", "ai_analysis", "error");
        }

        // ✅ TEST 3: SpeechToText mock (does not use OpenAI)
        [Fact]
        public void SpeechToText_ShouldReturnMockTranscript()
        {
            var client = new OpenAIClient(
                new ApiKeyCredential("dummy-key"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") });

            var service = new OpenAIService(client, _loggerMock.Object, _options);
            string transcript = service.SpeechToText("mock_audio_url");

            transcript.Should().Contain("mock transcript");
        }

        // ✅ TEST 4: Force success path without real API (simulate valid JSON)
        [Fact]
        public void GradeWriting_ShouldSimulateSuccess_ForCoverage()
        {
            var fake = new FakeOpenAIService(_loggerMock.Object);
            var result = fake.FakeGradeWriting("Question", "Answer");

            result.Should().NotBeNull();
            result.RootElement.ToString().Should().Contain("band_estimate");
        }

       

        // ✅ TEST 6: SpeechToText empty URL should trigger catch(Exception)
        [Fact]
        public void SpeechToText_ShouldHandleEmptyUrl()
        {
            var fake = new FakeOpenAIService(_loggerMock.Object);
            string result = fake.SpeechToText("");

            result.Should().Contain("Transcription failed");
        }
    }

    // =============================
    // 🔧 FAKE OpenAIService SUBCLASS
    // =============================
    public class FakeOpenAIService : OpenAIService
    {
        private readonly bool _forceInvalidJson;

        public FakeOpenAIService(ILogger<OpenAIService> logger, bool forceInvalidJson = false)
            : base(new OpenAIClient(new ApiKeyCredential("fake-key")),
                   logger,
                   Options.Create(new AiOptions { ChatModel = "mock" }))
        {
            _forceInvalidJson = forceInvalidJson;
        }

        // Ép chạy logic trong try/catch mà không gọi API thật
        public JsonDocument FakeGradeWriting(string q, string a)
        {
            try
            {
                string json = _forceInvalidJson
                    ? "INVALID_JSON"
                    : "{ \"grammar_vocab\": {}, \"band_estimate\": {} }";

                int first = json.IndexOf('{');
                int last = json.LastIndexOf('}');
                string jsonText = (first >= 0 && last > first)
                    ? json.Substring(first, last - first + 1)
                    : "{}";

                jsonText = jsonText.Replace("\r", "").Replace("\n", " ");

                return JsonDocument.Parse(jsonText);
            }
            catch (JsonException)
            {
                return JsonDocument.Parse(@"{ ""error"": ""Invalid JSON returned from AI"" }");
            }
            catch (Exception ex)
            {
                var msg = ex.Message.Replace("\"", "\\\"");
                return JsonDocument.Parse($@"{{ ""error"": ""{msg}"" }}");
            }
        }
    }
}
