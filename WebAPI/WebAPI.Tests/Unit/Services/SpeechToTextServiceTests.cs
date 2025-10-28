using System;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class SpeechToTextServiceTests
    {
        private SpeechToTextService CreateServiceWithKey()
        {
            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "dummy" }
            };
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            var logger = Mock.Of<ILogger<SpeechToTextService>>();
            var examService = Mock.Of<IExamService>();

            return new SpeechToTextService(config, logger, examService);
        }

        [Fact]
        public void Ctor_MissingApiKey_Throws()
        {
            IConfiguration config = new ConfigurationBuilder().Build();
            var logger = Mock.Of<ILogger<SpeechToTextService>>();
            var examService = Mock.Of<IExamService>();

            Action act = () => new SpeechToTextService(config, logger, examService);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Missing OpenAI API key*");
        }

        [Fact]
        public void TranscribeAndSave_EmptyUrl_Throws()
        {
            var service = CreateServiceWithKey();
            Action act = () => service.TranscribeAndSave(1, "");
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Audio URL is required*");
        }

        [Fact]
        public void TranscribeAndSave_InvalidUrl_ReturnsFailed()
        {
            var service = CreateServiceWithKey();
            var result = service.TranscribeAndSave(1, "ht!tp://bad-url");
            result.Should().Be("[Transcription failed]");
        }

        [Fact]
        public void TestCloudinaryAccess_InvalidUrl_ReturnsFalse()
        {
            var service = CreateServiceWithKey();
            var ok = service.TestCloudinaryAccess("ht!tp://bad-url");
            ok.Should().BeFalse();
        }

        [Fact]
        public void TranscribeFromFile_InvalidPath_Throws()
        {
            var service = CreateServiceWithKey();
            Action act = () => service.TranscribeFromFile("non_existent_file.wav", 1);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*File path is invalid or file does not exist*");
        }
    }
}


