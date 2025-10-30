using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.ClientModel;
using WebAPI.ExternalServices;
using Xunit;

namespace WebAPI.Tests.Unit.ExternalServices
{
    /// <summary>
    /// Full test for OpenAIService (unit + lightweight integration)
    /// Covers: GradeWriting, SpeechToText, GradeSpeaking
    /// </summary>
    public class OpenAIServiceTests
    {
        private readonly Mock<OpenAIClient> _clientMock;
        private readonly Mock<ChatClient> _chatClientMock;
        private readonly Mock<AudioClient> _audioClientMock;
        private readonly Mock<ILogger<OpenAIService>> _loggerMock;
        private readonly IOpenAIService _service;

        public OpenAIServiceTests()
        {
            _clientMock = new Mock<OpenAIClient>();
            _chatClientMock = new Mock<ChatClient>();
            _audioClientMock = new Mock<AudioClient>();
            _loggerMock = new Mock<ILogger<OpenAIService>>();

            _service = new OpenAIService(_clientMock.Object, _loggerMock.Object);
        }

        // ------------------------------------------------------------
        // 1️⃣ GradeWriting()
        // ------------------------------------------------------------

        [Fact]
        public void GradeWriting_ShouldReturnParsedJson_WhenResponseIsValid()
        {
            // Arrange
            var completion = CreateFakeChatCompletion("{\"band_estimate\":{\"overall\":8}}");
            _chatClientMock.Setup(c =>
                c.CompleteChat(It.IsAny<IEnumerable<ChatMessage>>(),
                               It.IsAny<ChatCompletionOptions>(),
                               It.IsAny<CancellationToken>()))
                .Returns(ClientResult.FromValue(completion, null));
            _clientMock.Setup(c => c.GetChatClient("gpt-4o"))
                .Returns(_chatClientMock.Object);

            // Act
            var doc = _service.GradeWriting("Q", "A");

            // Assert
            doc.RootElement.GetProperty("band_estimate")
                .GetProperty("overall").GetInt32().Should().Be(8);
        }

        [Fact]
        public void GradeWriting_ShouldReturnError_WhenChatThrows()
        {
            _clientMock.Setup(c => c.GetChatClient("gpt-4o"))
                .Throws(new Exception("network down"));

            var doc = _service.GradeWriting("Q", "A");

            doc.RootElement.GetProperty("error").GetString().Should().Contain("network down");
        }

        [Fact]
        public void GradeWriting_ShouldReturnError_WhenInvalidJsonReturned()
        {
            var completion = CreateFakeChatCompletion("NOT_VALID_JSON");
            _chatClientMock.Setup(c =>
                c.CompleteChat(It.IsAny<IEnumerable<ChatMessage>>(),
                               It.IsAny<ChatCompletionOptions>(),
                               It.IsAny<CancellationToken>()))
                .Returns(ClientResult.FromValue(completion, null));
            _clientMock.Setup(c => c.GetChatClient("gpt-4o"))
                .Returns(_chatClientMock.Object);

            var doc = _service.GradeWriting("Q", "A");

            doc.RootElement.TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public void GradeWriting_ShouldHandleImageUrl_Gracefully()
        {
            var completion = CreateFakeChatCompletion("{\"band_estimate\":{\"overall\":6}}");
            _chatClientMock.Setup(c =>
                c.CompleteChat(It.IsAny<IEnumerable<ChatMessage>>(),
                               It.IsAny<ChatCompletionOptions>(),
                               It.IsAny<CancellationToken>()))
                .Returns(ClientResult.FromValue(completion, null));
            _clientMock.Setup(c => c.GetChatClient("gpt-4o"))
                .Returns(_chatClientMock.Object);

            var doc = _service.GradeWriting("Q", "A", "https://example.com/img.png");

            doc.RootElement.GetProperty("band_estimate")
                .GetProperty("overall").GetInt32().Should().Be(6);
        }

        // ------------------------------------------------------------
        // 2️⃣ SpeechToText()
        // ------------------------------------------------------------

        [Fact]
        public void SpeechToText_ShouldReturnTranscript_WhenValidUrl()
        {
            var result = _service.SpeechToText("https://audio.com/file.mp3");
            result.Should().Contain("sample transcript");
        }

        [Fact]
        public void SpeechToText_ShouldReturnError_WhenEmptyUrl()
        {
            var result = _service.SpeechToText("");
            result.Should().Contain("[Transcription failed]");
        }

        [Fact]
        public void SpeechToText_ShouldReturnError_WhenThrowsException()
        {
            _clientMock.Setup(c => c.GetAudioClient(It.IsAny<string>()))
                .Throws(new Exception("audio error"));

            var result = _service.SpeechToText("abc");

            result.Should().Contain("[Transcription failed]");
        }

        // ------------------------------------------------------------
        // 3️⃣ GradeSpeaking()
        // ------------------------------------------------------------

        [Fact]
        public void GradeSpeaking_ShouldReturnValidJson()
        {
            var completion = CreateFakeChatCompletion("{\"band_estimate\":{\"overall\":7}}");
            _chatClientMock.Setup(c =>
                c.CompleteChat(It.IsAny<IEnumerable<ChatMessage>>(),
                               It.IsAny<ChatCompletionOptions>(),
                               It.IsAny<CancellationToken>()))
                .Returns(ClientResult.FromValue(completion, null));
            _clientMock.Setup(c => c.GetChatClient("gpt-4o"))
                .Returns(_chatClientMock.Object);

            var doc = _service.GradeSpeaking("Q", "Transcript");

            doc.RootElement.GetProperty("band_estimate")
                .GetProperty("overall").GetInt32().Should().Be(7);
        }

        [Fact]
        public void GradeSpeaking_ShouldReturnError_WhenInvalidJson()
        {
            var completion = CreateFakeChatCompletion("INVALID");
            _chatClientMock.Setup(c =>
                c.CompleteChat(It.IsAny<IEnumerable<ChatMessage>>(),
                               It.IsAny<ChatCompletionOptions>(),
                               It.IsAny<CancellationToken>()))
                .Returns(ClientResult.FromValue(completion, null));
            _clientMock.Setup(c => c.GetChatClient("gpt-4o"))
                .Returns(_chatClientMock.Object);

            var doc = _service.GradeSpeaking("Q", "T");

            doc.RootElement.TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public void GradeSpeaking_ShouldReturnError_WhenThrows()
        {
            _clientMock.Setup(c => c.GetChatClient(It.IsAny<string>()))
                .Throws(new Exception("api fail"));

            var doc = _service.GradeSpeaking("Q", "T");

            doc.RootElement.GetProperty("error").GetString().Should().Contain("api fail");
        }

        // ------------------------------------------------------------
        // 4️⃣ Integration-like sanity test (no mocks)
        // ------------------------------------------------------------
        [Fact]
        public void Integration_FakeClient_ShouldReturnSafeFallback()
        {
            var realLogger = Mock.Of<ILogger<OpenAIService>>();
            var fakeClient = new Mock<OpenAIClient>().Object;

            var service = new OpenAIService(fakeClient, realLogger);

            var result = service.SpeechToText("test.mp3");
            result.Should().Contain("sample transcript");
        }

      
        private static ChatCompletion CreateFakeChatCompletion(string content)
        {
            var msg = new ChatMessageContent(content);
            var completion = (ChatCompletion)Activator.CreateInstance(
                typeof(ChatCompletion),
                nonPublic: true)!;
            typeof(ChatCompletion)
                .GetProperty("Content")?
                .SetValue(completion, new List<ChatMessageContent> { msg });
            return completion;
        }
    }
}
