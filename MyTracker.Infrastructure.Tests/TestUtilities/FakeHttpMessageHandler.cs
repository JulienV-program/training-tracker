using System.Net;

namespace MyTracker.Infrastructure.Tests.TestUtilities;

/// <summary>
/// Remplace le réseau dans les tests : répond via une fonction et enregistre chaque requête envoyée
/// (méthode, URL, en-têtes, corps) pour les assertions.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        : this(_ => new HttpResponseMessage(statusCode) { Content = new StringContent(responseContent) }) { }

    public List<RecordedRequest> Requests { get; } = [];

    public RecordedRequest? LastRequest => Requests.LastOrDefault();
    public string? LastRequestBody => LastRequest?.Body;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), body));
        return _responder(request);
    }

    public record RecordedRequest(HttpMethod Method, Uri RequestUri, string? Authorization, string? Body);
}
