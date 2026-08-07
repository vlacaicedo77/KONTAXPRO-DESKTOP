using System.Net.Http.Json;
using System.Text.Json;

namespace KONTAXPRO.Infrastructure.Interoperabilidad;

public sealed class GuiaTokenClient(
    IHttpClientFactory httpClientFactory,
    InteroperabilidadOptions options,
    TimeProvider timeProvider)
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public async Task<string?> GetAccessTokenAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && IsCurrentTokenValid())
            return _accessToken;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && IsCurrentTokenValid())
                return _accessToken;

            var configuration = options.Guia;
            if (!configuration.HasCredentials ||
                !Uri.TryCreate(configuration.TokenUrl, UriKind.Absolute, out var uri))
                return null;

            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = configuration.ClientId,
                    ["client_secret"] = configuration.ClientSecret,
                    ["grant_type"] = "client_credentials"
                })
            };

            var client = httpClientFactory.CreateClient(
                InteroperabilidadHttpClients.GuiaToken);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            using var document = await JsonResponseReader.TryReadDocumentAsync(
                response.Content,
                cancellationToken);
            if (document is null)
                return null;
            var token = JsonResponseReader.GetString(
                document.RootElement,
                "access_token",
                "accessToken");
            var expiresText = JsonResponseReader.GetString(
                document.RootElement,
                "expires_in",
                "expiresIn");

            if (string.IsNullOrWhiteSpace(token))
                return null;

            var expiresSeconds = int.TryParse(expiresText, out var parsed)
                ? Math.Max(parsed, 60)
                : 300;
            var safetySeconds = Math.Min(60, Math.Max(10, expiresSeconds / 10));
            _accessToken = token;
            _expiresAt = timeProvider.GetUtcNow()
                .AddSeconds(expiresSeconds - safetySeconds);
            return _accessToken;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void Invalidate(string? token)
    {
        if (token is null || string.Equals(token, _accessToken, StringComparison.Ordinal))
        {
            _accessToken = null;
            _expiresAt = default;
        }
    }

    private bool IsCurrentTokenValid() =>
        !string.IsNullOrWhiteSpace(_accessToken) &&
        timeProvider.GetUtcNow() < _expiresAt;
}
