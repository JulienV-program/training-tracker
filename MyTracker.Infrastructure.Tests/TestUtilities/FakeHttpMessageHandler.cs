using System.Net;

namespace MyTracker.Infrastructure.Tests.TestUtilities;

public class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseContent) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content != null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseContent)
        };
    }
}
