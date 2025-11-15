using Moq;
using Moq.Protected;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class HttpMessageHandlerMock
{
    public Mock<HttpMessageHandler> HandlerMock { get; }

    public HttpMessageHandlerMock()
    {
        HandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    }

    public void SetupResponse(HttpResponseMessage response)
    {
        HandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response)
            .Verifiable();
    }

    public HttpClient CreateClient(string baseUrl = "http://localhost")
    {
        var client = new HttpClient(HandlerMock.Object);
        client.BaseAddress = new Uri(baseUrl);
        return client;
    }
}
