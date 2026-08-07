using System.Net;
using System.Text;

namespace KONTAXPRO.Tests.Clientes;

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly IReadOnlyDictionary<string, HttpClient> _clients;

    public StubHttpClientFactory(IReadOnlyDictionary<string, HttpClient> clients)
    {
        _clients = clients;
    }

    public HttpClient CreateClient(string name) => _clients[name];
}

internal sealed class SequenceHttpMessageHandler(
    Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> send)
    : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        return send(request, CallCount, cancellationToken);
    }
}

internal static class HttpResponses
{
    public static HttpResponseMessage Json(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
}
