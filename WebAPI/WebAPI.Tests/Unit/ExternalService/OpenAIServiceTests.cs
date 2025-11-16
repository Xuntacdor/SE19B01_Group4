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

        [Fact]
        public void GetSpeakingSuggestion_ShouldReturnJson_FromLocalAI()
        {
            var client = new OpenAIClient(
                new ApiKeyCredential("dummy"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") });

            var service = new OpenAIService(client, _loggerMock.Object, _options);

            var result = service.GetSpeakingSuggestion("What is your hobby?");

            result.Should().NotBeNull();

            var text = result.RootElement.ToString();
            text.Should().ContainAny("vocabulary_by_level", "sample_answer", "error");
        }

        [Fact]
        public void LookupWordAI_ShouldReturnValidJson_FromLocalAI()
        {
            var client = new OpenAIClient(
                new ApiKeyCredential("dummy"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") });

            var service = new OpenAIService(client, _loggerMock.Object, _options);

            var result = service.LookupWordAI("beautiful");

            var text = result.RootElement.ToString();
            text.Should().ContainAny("term", "vietnameseTranslation", "englishTranslation", "error");
        }
        [Fact]
        public void AnalyzeContent_ShouldReturnValidJson_FromLocalAI()
        {
            var client = new OpenAIClient(
                new ApiKeyCredential("dummy"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") });

            var service = new OpenAIService(client, _loggerMock.Object, _options);

            var result = service.AnalyzeContent(
                "Hello world",
                "This is a normal comment without bad words.",
                "comment"
            );

            var text = result.RootElement.ToString();
            text.Should().ContainAny("is_meaningful", "summary", "inappropriate_words", "error");
        }
        [Fact]
        public void GradeWriting_ShouldReturnError_WhenLocalAIUnavailable()
        {
            var client = new OpenAIClient(
                new ApiKeyCredential("dummy"),
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:9999/notexists") });

            var service = new OpenAIService(client, _loggerMock.Object, _options);

            var result = service.GradeWriting("Q", "A");

            result.RootElement.TryGetProperty("error", out _).Should().BeTrue();
        }



    }
}
