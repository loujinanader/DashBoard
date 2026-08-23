using DashBoard.Models.Glpi;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DashBoard.Service
{
    public class GLPIService : IGLPIService
    {
        // Serializes token refreshes across concurrent requests: GLPI rotates the
        // refresh_token on every use, so two requests refreshing at once would race
        // and the second would be refused (its refresh_token already spent).
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _tokenFilePath;

        public GLPIService(HttpClient httpClient, IConfiguration configuration, IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _tokenFilePath = Path.Combine(env.ContentRootPath, "App_Data", "glpi-token.json");
        }

        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var accessToken = await GetFreshAccessTokenAsync();

            var url = $"{RequireConfig("GLPI:ApiBaseUrl")}/Assistance/Ticket";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"GLPI error: {response.StatusCode} - {json}");

            return JsonSerializer.Deserialize<List<Ticket>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Ticket>();
        }

        public string GetAuthorizationUrl()
        {
            var clientId = RequireConfig("GLPI:ClientId");
            var redirectUri = RequireConfig("GLPI:RedirectUri");
            var authorizationUrl = RequireConfig("GLPI:AuthorizationUrl");

            var query = $"response_type=code" +
                        $"&client_id={Uri.EscapeDataString(clientId)}" +
                        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}";

            return $"{authorizationUrl}?{query}";
        }

        // One-time bootstrap: exchanges the authorization_code obtained via the GLPI
        // consent redirect for the first refresh_token, then stores it. After this,
        // GetTicketsAsync no longer needs the authorization_code flow.
        public async Task ExchangeAuthorizationCodeAsync(string code)
        {
            var clientId = RequireConfig("GLPI:ClientId");
            var clientSecret = RequireConfig("GLPI:ClientSecret");
            var redirectUri = RequireConfig("GLPI:RedirectUri");
            var tokenUrl = RequireConfig("GLPI:TokenUrl");

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicCredentials(clientId, clientSecret));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"GLPI token error: {response.StatusCode} - {json}");

            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);

            if (!tokenResponse.TryGetProperty("refresh_token", out var refreshTokenElement))
                throw new Exception($"GLPI did not return a refresh token. Response: {json}");

            var refreshToken = refreshTokenElement.GetString()
                ?? throw new Exception("GLPI refresh token is empty.");

            await WriteStoredRefreshTokenAsync(refreshToken);
        }

        // Exchanges the stored refresh_token for a brand-new access_token on every call.
        // GLPI's OAuth server rotates the refresh_token on each use, so whatever new one
        // comes back is persisted immediately, before it's ever used again.
        private async Task<string> GetFreshAccessTokenAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                var refreshToken = await ReadStoredRefreshTokenAsync()
                    ?? throw new GlpiNotAuthorizedException(
                        "No GLPI refresh token stored yet. Visit /auth/glpi/login once to authorize this app.");

                var clientId = RequireConfig("GLPI:ClientId");
                var clientSecret = RequireConfig("GLPI:ClientSecret");
                var tokenUrl = RequireConfig("GLPI:TokenUrl");

                using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicCredentials(clientId, clientSecret));
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken
                });

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new GlpiNotAuthorizedException(
                        $"GLPI rejected the stored refresh token ({response.StatusCode} - {json}). " +
                        "Visit /auth/glpi/login to reauthorize.");

                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);

                if (tokenResponse.TryGetProperty("refresh_token", out var newRefreshTokenElement))
                {
                    var newRefreshToken = newRefreshTokenElement.GetString();
                    if (!string.IsNullOrWhiteSpace(newRefreshToken))
                        await WriteStoredRefreshTokenAsync(newRefreshToken);
                }

                if (!tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
                    throw new Exception($"GLPI did not return an access token. Response: {json}");

                return accessTokenElement.GetString()
                    ?? throw new Exception("GLPI access token is empty.");
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<string?> ReadStoredRefreshTokenAsync()
        {
            if (!File.Exists(_tokenFilePath))
                return null;

            var json = await File.ReadAllTextAsync(_tokenFilePath);
            var stored = JsonSerializer.Deserialize<StoredToken>(json);
            return stored?.RefreshToken;
        }

        private async Task WriteStoredRefreshTokenAsync(string refreshToken)
        {
            var directory = Path.GetDirectoryName(_tokenFilePath)!;
            Directory.CreateDirectory(directory);

            var stored = new StoredToken(refreshToken, DateTimeOffset.UtcNow);
            await File.WriteAllTextAsync(_tokenFilePath, JsonSerializer.Serialize(stored));
        }

        private static string BasicCredentials(string clientId, string clientSecret) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        private string RequireConfig(string key) =>
            _configuration[key] is { Length: > 0 } value
                ? value
                : throw new InvalidOperationException($"{key} is not configured.");

        private record StoredToken(string RefreshToken, DateTimeOffset UpdatedAtUtc);
    }
}
