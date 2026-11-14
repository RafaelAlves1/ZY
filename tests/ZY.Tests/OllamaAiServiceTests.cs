
using System.Net;
using System.Text;
using ZY.Infrastructure.AI;

namespace ZY.Tests;

public sealed class OllamaAiServiceTests
{
    [Fact]
    public async Task GenerateResponseAsync_ShouldReturnAiResponse()
    {
        // Arrange
        var responseJson = """
            {
                "message": {
                    "role": "assistant",
                    "content": "Olá! Sou o ZY."
                }
            }
            """;

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            responseJson
        );

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var aiService = new OllamaAiService(httpClient);

        // Act
        var response = await aiService.GenerateResponseAsync([]);

        // Assert
        Assert.Equal("Olá! Sou o ZY.", response);
    }


    [Fact]
    public async Task GenerateResponseAsync_ShouldThrowWhenModelNotFound()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.NotFound,
            """{"error":"model 'qwen2.5:1.5b' not found"}"""
        );

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var aiService = new OllamaAiService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<OllamaModelNotFoundException>(
            () => aiService.GenerateResponseAsync([])
        );
    }


    [Fact]
    public async Task GenerateResponseAsync_ShouldThrowHttpRequestExceptionOnServerError()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            """{"error":"internal server error"}"""
        );

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var aiService = new OllamaAiService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => aiService.GenerateResponseAsync([])
        );
    }



    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;

        public FakeHttpMessageHandler(
            HttpStatusCode statusCode,
            string responseContent
        )
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _responseContent,
                    Encoding.UTF8,
                    "application/json"
                )
            };

            return Task.FromResult(response);
        }
    }
}
