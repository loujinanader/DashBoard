using DashBoard.Models.Glpi;
using System.Net.Http.Headers;
using System.Text.Json;
namespace DashBoard.Service.GlpiServices
{
    public class GLPIService : IGLPIService
    {
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
            var itUserIds = _configuration
                .GetSection("GLPI:ITUserIds")
                .Get<int[]>() ?? Array.Empty<int>();
            var itTickets = tickets
                .Where(t => t.Team != null &&
                            t.Team.Any(member => itUserIds.Contains(member.Id)))
                .ToList();
            return itTickets;
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