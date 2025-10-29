using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebAPI.Models;
using WebAPI.Services;
using WebAPI.DTOs;
using System.Net.Http;
using System.Text;

namespace WebAPI.Tests.Unit.ExternalService;

public class SpeechToTextServiceTests
{
    private class FakeExamService : IExamService
    {
        public ExamAttempt? AttemptToReturn { get; set; }

        public Exam? GetById(int id) => throw new NotImplementedException();
        public List<Exam> GetAll() => throw new NotImplementedException();
        public Exam Create(CreateExamDto exam) => throw new NotImplementedException();
        public Exam? Update(int id, UpdateExamDto exam) => throw new NotImplementedException();
        public bool Delete(int id) => throw new NotImplementedException();
        public ExamAttempt? GetAttemptById(long attemptId) => AttemptToReturn;
        public List<ExamAttemptSummaryDto> GetExamAttemptsByUser(int userId) => throw new NotImplementedException();
        public ExamAttemptDto? GetExamAttemptDetail(long attemptId) => throw new NotImplementedException();
        public ExamAttempt SubmitAttempt(SubmitAttemptDto dto, int userId) => throw new NotImplementedException();
        public void Save() { }
    }

    private static ILogger<SpeechToTextService> CreateLogger()
    {
        return LoggerFactory.Create(builder => { }).CreateLogger<SpeechToTextService>();
    }

    private static IConfiguration CreateConfigWithoutKey()
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
    }

    private static IConfiguration CreateConfigWithKey()
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = "test-key"
        }).Build();
    }

    [Fact]
    public void Constructor_Throws_WhenApiKeyMissing()
    {
        var config = CreateConfigWithoutKey();
        var logger = CreateLogger();
        var examService = new FakeExamService();

        Assert.Throws<ArgumentException>(() => new SpeechToTextService(config, logger, examService));
    }

    [Fact]
    public void Constructor_Succeeds_WhenApiKeyProvided()
    {
        var config = CreateConfigWithKey();
        var logger = CreateLogger();
        var examService = new FakeExamService();

        var service = new SpeechToTextService(config, logger, examService);
        Assert.NotNull(service);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "")]
    [InlineData(2, "  ")]
    public void TranscribeAndSave_Throws_OnEmptyOrNullUrl(long attemptId, string? audioUrl)
    {
        var service = new SpeechToTextService(CreateConfigWithKey(), CreateLogger(), new FakeExamService());

        Assert.Throws<ArgumentException>(() => service.TranscribeAndSave(attemptId, audioUrl!));
    }

    [Fact]
    public void TranscribeFromFile_Throws_OnInvalidFilePath()
    {
        var service = new SpeechToTextService(CreateConfigWithKey(), CreateLogger(), new FakeExamService());

        Assert.Throws<ArgumentException>(() => service.TranscribeFromFile("Z:/definitely/not/exist/file.webm", 123));
    }

    // ----------------- Helpers for advanced HTTP path tests -----------------
    private static SpeechToTextService CreateServiceWithHttp(IConfiguration config, ILogger<SpeechToTextService> logger, FakeExamService examService, Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var service = new SpeechToTextService(config, logger, examService);

        var httpClientField = typeof(SpeechToTextService).GetField("_http", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var existing = (HttpClient?)httpClientField!.GetValue(service);
        existing?.Dispose();

        var client = new HttpClient(new CustomHandler(handler));
        httpClientField.SetValue(service, client);
        return service;
    }

    private sealed class CustomHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _logic;
        public CustomHandler(Func<HttpRequestMessage, HttpResponseMessage> logic)
        {
            _logic = logic;
        }
        protected override HttpResponseMessage Send(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            => _logic(request);
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            => System.Threading.Tasks.Task.FromResult(_logic(request));
    }

    [Fact]
    public void TranscribeAndSave_ReturnsTranscript_OnSuccess_AllowsNoExtUrl()
    {
        // URL without extension triggers default to .webm and MIME mapping
        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var ok = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("audio"))
                };
                ok.Content.Headers.ContentLength = 5;
                return ok;
            }

            // POST to OpenAI responds with a transcript
            var okPost = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"text\":\"hello from webm\"}", Encoding.UTF8, "application/json")
            };
            return okPost;
        });

        var result = service.TranscribeAndSave(1, "http://cdn.example.com/path/noext");
        Assert.Equal("hello from webm", result);
    }

    [Fact]
    public void TranscribeAndSave_UnsupportedExtGetsNormalized_ReturnsTranscript()
    {
        // .abc should be converted to .webm in helper branch
        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var ok = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 0x01, 0x02 })
                };
                ok.Content.Headers.ContentLength = 2;
                return ok;
            }
            var okPost = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"text\":\"normalized\"}", Encoding.UTF8, "application/json")
            };
            return okPost;
        });

        var result = service.TranscribeAndSave(1, "http://cdn.example.com/file.abc");
        Assert.Equal("normalized", result);
    }

    [Fact]
    public void TranscribeAndSave_ReturnsFailed_WhenDownloadFails()
    {
        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var result = service.TranscribeAndSave(1, "http://cdn.example.com/file.mp3");
        Assert.Contains("failed", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TranscribeAndSave_ReturnsFailed_WhenWhisperErrors_WithJsonBody()
    {
        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var ok = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 0x03 })
                };
                ok.Content.Headers.ContentLength = 1;
                return ok;
            }
            var bad = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":{\"type\":\"bad_request\",\"message\":\"oops\"}}", Encoding.UTF8, "application/json")
            };
            return bad;
        });

        var result = service.TranscribeAndSave(1, "http://cdn.example.com/file.mp3");
        Assert.Contains("failed", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestCloudinaryAccess_True_And_False()
    {
        var fakeExam = new FakeExamService();
        var serviceOk = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        Assert.True(serviceOk.TestCloudinaryAccess("http://x"));

        var serviceFail = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, _ => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
        Assert.False(serviceFail.TestCloudinaryAccess("http://x"));
    }

    [Fact]
    public void TranscribeFromFile_Succeeds_SavesToAttempt_WhenPresent()
    {
        string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".mp3");
        System.IO.File.WriteAllText(tmp, "dummy");

        var fakeExam = new FakeExamService { AttemptToReturn = new ExamAttempt() };
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            // Only POST is used in TranscribeFromFile
            var okPost = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"text\":\"from file\"}", Encoding.UTF8, "application/json")
            };
            return okPost;
        });

        var result = service.TranscribeFromFile(tmp, 123);
        Assert.Equal("from file", result);

        System.IO.File.Delete(tmp);
    }

    [Fact]
    public void TranscribeFromFile_ReturnsFailed_WhenWhisperFails()
    {
        string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
        System.IO.File.WriteAllBytes(tmp, new byte[] { 0x01, 0x02, 0x03 });

        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            var bad = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":{\"message\":\"bad\"}}", Encoding.UTF8, "application/json")
            };
            return bad;
        });

        var result = service.TranscribeFromFile(tmp, 1);
        Assert.Contains("failed", result, StringComparison.OrdinalIgnoreCase);

        System.IO.File.Delete(tmp);
    }

    [Theory]
    [InlineData(".mp4")]
    [InlineData(".m4a")]
    [InlineData(".ogg")]
    [InlineData(".oga")]
    [InlineData(".flac")]
    [InlineData(".mpga")]
    [InlineData(".unknown")] // default branch
    public void TranscribeAndSave_Hits_All_Mime_Branches(string ext)
    {
        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var ok = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 0x00 })
                };
                ok.Content.Headers.ContentLength = 1;
                return ok;
            }
            var okPost = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"text\":\"ok\"}", Encoding.UTF8, "application/json")
            };
            return okPost;
        });

        var url = "http://cdn.example.com/file" + ext;
        var result = service.TranscribeAndSave(1, url);
        Assert.Equal("ok", result);
    }

    [Fact]
    public void TranscribeAndSave_ReturnsFailed_WhenWhisperErrors_WithInvalidBody()
    {
        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var ok = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 0x10 })
                };
                ok.Content.Headers.ContentLength = 1;
                return ok;
            }
            // invalid (non-JSON) body to trigger parse catch
            var bad = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("<html>bad</html>", Encoding.UTF8, "text/html")
            };
            return bad;
        });

        var result = service.TranscribeAndSave(1, "http://cdn.example.com/file.mp3");
        Assert.Contains("failed", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TranscribeFromFile_ReturnsFailed_WhenWhisperErrors_WithInvalidBody()
    {
        string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".m4a");
        System.IO.File.WriteAllBytes(tmp, new byte[] { 0x22, 0x33 });

        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, req =>
        {
            var bad = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("not json", Encoding.UTF8, "text/plain")
            };
            return bad;
        });

        var result = service.TranscribeFromFile(tmp, 1);
        Assert.Contains("failed", result, StringComparison.OrdinalIgnoreCase);

        System.IO.File.Delete(tmp);
    }

    [Fact]
    public void TestCloudinaryAccess_ReturnsFalse_OnException()
    {
        var fakeExam = new FakeExamService();
        var service = CreateServiceWithHttp(CreateConfigWithKey(), CreateLogger(), fakeExam, _ => throw new InvalidOperationException("boom"));

        Assert.False(service.TestCloudinaryAccess("http://x"));
    }
}
