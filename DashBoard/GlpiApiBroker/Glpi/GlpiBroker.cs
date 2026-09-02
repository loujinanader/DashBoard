using DashBoard.Exceptions;
using DashBoard.Models.Glpi;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
namespace DashBoard.GlpiApiBroker.Glpi
{
    public class GLPIBroker : IGLPIBroker
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public GLPIBroker(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
        private const int PageSize = 500;
        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var accessToken = await GetAccessTokenAsync();
            var baseUrl =
                $"{RequireConfig("GLPI:ApiBaseUrl")}/Assistance/Ticket";
            var tickets = new List<Ticket>();
            var start = 0;
            var total = int.MaxValue;
            while (start < total)
            {
                var url =
                    $"{baseUrl}?start={start}&limit={PageSize}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new GlpiAuthenticationException($"GLPI authentication failed: {json}");
                if (!response.IsSuccessStatusCode)
                    throw new GlpiApiException(response.StatusCode, $"GLPI API error: {json}");
                var page = JsonSerializer.Deserialize<List<Ticket>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
                    ?? new List<Ticket>();
                if (page.Count == 0)
                    break;
                tickets.AddRange(page);
                start += page.Count;
                total = TryGetContentRangeTotal(response)
                    ?? tickets.Count;
            }
            return tickets;
        }
        public async Task<string> GetAccessTokenAsync()
        {
            var clientId =
                RequireConfig("GLPI:ClientId");
            var clientSecret =
                RequireConfig("GLPI:ClientSecret");
            var username =
                RequireConfig("GLPI:Username");
            var password =
                RequireConfig("GLPI:Password");
            var tokenUrl =
                RequireConfig("GLPI:TokenUrl");
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
            return accessTokenElement.GetString()
                   ?? throw new Exception("GLPI access token is empty.");
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
        private string RequireConfig(string key) => _configuration[key] is { Length: > 0 } value
                ? value
                : throw new GlpiConfigurationException(
                    $"{key} is not configured.");
    }
}
