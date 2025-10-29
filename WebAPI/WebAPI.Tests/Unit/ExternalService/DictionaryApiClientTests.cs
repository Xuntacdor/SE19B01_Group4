using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using WebAPI.ExternalServices;
using WebAPI.Models;
using Xunit;

namespace WebAPI.Tests.Unit.ExternalServices
{
    public class DictionaryApiClientTests
    {
        private static HttpClient CreateHttpClientWithResponse(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);
            return new HttpClient(handlerMock.Object);
        }
        [Fact]
        public void GetWord_ReturnsNull_WhenResponseNotSuccessful()
        {
            var httpClient = CreateHttpClientWithResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
            var api = new DictionaryApiClient(httpClient);
            var result = api.GetWord("test");
            result.Should().BeNull();
        }
        [Fact]
        public void GetWord_ReturnsNull_WhenJsonNotArray()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"word\":\"test\"}")
            };
            var httpClient = CreateHttpClientWithResponse(response);
            var api = new DictionaryApiClient(httpClient);

            var result = api.GetWord("test");

            result.Should().BeNull();
        }
        [Fact]
        public void GetWord_ReturnsNull_WhenArrayEmpty()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            };
            var httpClient = CreateHttpClientWithResponse(response);
            var api = new DictionaryApiClient(httpClient);
            var result = api.GetWord("test");
            result.Should().BeNull();
        }
        [Fact]
        public void GetWord_ParsesWordWithMeaningExampleAndAudio()
        {
            var json = @"[
                {
                    ""word"": ""apple"",
                    ""meanings"": [
                        {
                            ""definitions"": [
                                { ""definition"": ""A fruit."", ""example"": ""I ate an apple."" }
                            ]
                        }
                    ],
                    ""phonetics"": [
                        { ""audio"": ""https://audio.url/apple.mp3"" }
                    ]
                }
            ]";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            var httpClient = CreateHttpClientWithResponse(response);
            var api = new DictionaryApiClient(httpClient);
            var result = api.GetWord("apple");
            result.Should().NotBeNull();
            result!.Term.Should().Be("apple");
            result.Meaning.Should().Be("A fruit.");
            result.Example.Should().Be("I ate an apple.");
            result.Audio.Should().Be("https://audio.url/apple.mp3");
        }
        [Fact]
        public void GetWord_ParsesWordWithoutAudioOrExample()
        {
            var json = @"[
                {
                    ""word"": ""book"",
                    ""meanings"": [
                        {
                            ""definitions"": [
                                { ""definition"": ""A set of pages."" }
                            ]
                        }
                    ],
                    ""phonetics"": []
                }
            ]";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            var httpClient = CreateHttpClientWithResponse(response);
            var api = new DictionaryApiClient(httpClient);
            var result = api.GetWord("book");
            result.Should().NotBeNull();
            result!.Term.Should().Be("book");
            result.Meaning.Should().Be("A set of pages.");
            result.Example.Should().BeNull();
            result.Audio.Should().BeNull();
        }
    }
}
