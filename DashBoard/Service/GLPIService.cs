using DashBoard.Models.Glpi;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace DashBoard.Service
{
    public class GLPIService : IGLPIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public GLPIService(HttpClient httpClient, IConfiguration configuration)
        {
            this._httpClient = httpClient;
            _configuration = configuration;
        }
        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var clientId = _configuration["GLPI:ClientId"];
            var clientSecret = _configuration["GLPI:ClientSecret"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("GLPI ClientId and ClientSecret are not configured.");

            // Get access token using configured credentials
            var token = await GetAccessTokenAsync();

            var url = $"{_configuration["GLPI:ApiBaseUrl"]}/Assistance/Ticket";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"GLPI error: {response.StatusCode} - {json}");

            return JsonSerializer.Deserialize<List<Ticket>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Ticket>();
        }
        private async Task<string> GetAccessTokenAsync()
        {
            var clientId = _configuration["GLPI:ClientId"];
            var clientSecret = _configuration["GLPI:ClientSecret"];
            var authorizationCode = _configuration["GLPI:AuthorizationCode"];
            var redirectUri = _configuration["GLPI:RedirectUri"];
            var tokenUrl = _configuration["GLPI:TokenUrl"];

            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("GLPI ClientId is missing.");

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("GLPI ClientSecret is missing.");

            if (string.IsNullOrWhiteSpace(authorizationCode))
                throw new InvalidOperationException("GLPI AuthorizationCode is missing.");

            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new InvalidOperationException("GLPI RedirectUri is missing.");

            if (string.IsNullOrWhiteSpace(tokenUrl))
                throw new InvalidOperationException("GLPI TokenUrl is missing.");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                tokenUrl);

            // OAuth client authentication
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            request.Content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = authorizationCode,
                    ["redirect_uri"] = redirectUri
                });

            var response = await _httpClient.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(
                    $"GLPI token error: {response.StatusCode} - {json}");

            var tokenResponse =
                JsonSerializer.Deserialize<JsonElement>(json);

            if (!tokenResponse.TryGetProperty(
                    "access_token",
                    out var accessToken))
            {
                throw new Exception(
                    $"GLPI did not return an access token. Response: {json}");
            }

            return accessToken.GetString()
                ?? throw new Exception("GLPI access token is empty.");
        }
    }
}
