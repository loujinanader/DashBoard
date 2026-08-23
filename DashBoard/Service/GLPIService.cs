using DashBoard.Models.Glpi;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DashBoard.Service
{
    public class GLPIService : IGLPIService
    {
        // Serializes access-token fetches across concurrent requests so a burst of
        // requests hitting an expired cached token doesn't fire off duplicate
        // password-grant token requests.
        private static readonly SemaphoreSlim _tokenLock = new(1, 1);

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private string? _cachedAccessToken;
        private DateTimeOffset _cachedAccessTokenExpiresAt = DateTimeOffset.MinValue;

        public GLPIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // GLPI paginates Assistance/Ticket at 100 items per page by default (a plain
        // GET returns "206 Partial Content" with a "Content-Range: start-end/total"
        // header). We page through with start/limit until every item is collected,
        // rather than requesting one large limit that could still be short of a
        // future ticket count or an instance-side cap.
        private const int PageSize = 500;

        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var accessToken = await GetAccessTokenAsync();
            var baseUrl = $"{RequireConfig("GLPI:ApiBaseUrl")}/Assistance/Ticket";

            var tickets = new List<Ticket>();
            var start = 0;
            var total = int.MaxValue;

            while (start < total)
            {
                var url = $"{baseUrl}?start={start}&limit={PageSize}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"GLPI error: {response.StatusCode} - {json}");

                var page = JsonSerializer.Deserialize<List<Ticket>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Ticket>();

                if (page.Count == 0)
                    break;

                tickets.AddRange(page);
                start += page.Count;
                total = TryGetContentRangeTotal(response) ?? tickets.Count;
            }

            return tickets;
        }

        private static int? TryGetContentRangeTotal(HttpResponseMessage response)
        {
            if (!response.Content.Headers.TryGetValues("Content-Range", out var values))
                return null;

            var value = values.FirstOrDefault();
            var slashIndex = value?.LastIndexOf('/') ?? -1;

            return slashIndex >= 0 && int.TryParse(value.AsSpan(slashIndex + 1), out var total)
                ? total
                : null;
        }

        // This GLPI instance's high-level API only accepts OAuth2 "authorization_code"
        // or "password" as valid security schemes (confirmed via its own
        // /api.php/doc.json) -- client_credentials tokens are issuable but rejected
        // by every protected route, since they carry no GLPI user identity. "password"
        // trades a technical account's username/password for a token directly, no
        // browser needed, and the resulting token acts as that GLPI user. There's no
        // refresh_token either way here, so we just cache the access_token in memory
        // until shortly before it expires, then request a fresh one.
        private async Task<string> GetAccessTokenAsync()
        {
            if (_cachedAccessToken is not null && DateTimeOffset.UtcNow < _cachedAccessTokenExpiresAt)
                return _cachedAccessToken;

            await _tokenLock.WaitAsync();
            try
            {
                if (_cachedAccessToken is not null && DateTimeOffset.UtcNow < _cachedAccessTokenExpiresAt)
                    return _cachedAccessToken;

                var clientId = RequireConfig("GLPI:ClientId");
                var clientSecret = RequireConfig("GLPI:ClientSecret");
                var username = RequireConfig("GLPI:Username");
                var password = RequireConfig("GLPI:Password");
                var tokenUrl = RequireConfig("GLPI:TokenUrl");

                using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
                var payload = new
                {
                    grant_type = "password",
                    client_id = clientId,
                    client_secret = clientSecret,
                    username = username,
                    password = password,
                    scope = "api"
                };
                // GLPI's token endpoint only recognizes a bare "application/json"
                // Content-Type; StringContent's 3-arg constructor appends
                // "; charset=utf-8", which makes GLPI treat the body as empty and
                // reply "unsupported_grant_type" instead of parsing it.
                request.Content = new StringContent(JsonSerializer.Serialize(payload));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"GLPI token error: {response.StatusCode} - {json}");

                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);

                if (!tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
                    throw new Exception($"GLPI did not return an access token. Response: {json}");

                var accessToken = accessTokenElement.GetString()
                    ?? throw new Exception("GLPI access token is empty.");

                var expiresIn = tokenResponse.TryGetProperty("expires_in", out var expiresInElement)
                    ? expiresInElement.GetInt32()
                    : 3600;

                _cachedAccessToken = accessToken;
                // Refresh a bit early so a slow request never gets rejected mid-flight.
                _cachedAccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 30);

                return accessToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private string RequireConfig(string key) =>
            _configuration[key] is { Length: > 0 } value
                ? value
                : throw new InvalidOperationException($"{key} is not configured.");
    }
}
