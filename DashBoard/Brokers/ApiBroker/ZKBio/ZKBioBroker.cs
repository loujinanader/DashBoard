using DashBoard.Exceptions;
using DashBoard.Models.Glpi;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DashBoard.Brokers.ApiBroker.ZKBio
{
    public class ZKBioBroker : IZKBioBroker
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public ZKBioBroker(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
        private const int PageSize = 500;
        public async Task<List<Ticket>> GetFingerPrintsAsync()
        {
            var accessToken = await GetAccessTokenAsync();
            var baseUrl =
                $"{RequireConfig("ZKBio:ApiBaseUrl")}/iclock/api/transactions";
            var tickets = new List<Ticket>();
            var start = 0;
            var total = int.MaxValue;
            while (start < total)
            {
                var url =
                    $"{baseUrl}?start={start}&limit={PageSize}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("JWT", accessToken);
                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new ZKBioAuthenticationException($"ZKBio authentication failed: {json}");
                if (!response.IsSuccessStatusCode)
                    throw new ZKBioApiException(response.StatusCode, $"ZKBio API error: {json}");
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
                RequireConfig("ZKBio:ClientId");
            var clientSecret =
                RequireConfig("ZKBio:ClientSecret");
            var username =
                RequireConfig("ZKBio:Username");
            var password =
                RequireConfig("ZKBio:Password");
            var tokenUrl =
                RequireConfig("ZKBio:TokenUrl");
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
                throw new Exception($"ZKBio token error: {response.StatusCode} - {json}");
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);
            if (!tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
                throw new Exception($"ZKBio did not return an access token. Response: {json}");
            return accessTokenElement.GetString()
                   ?? throw new Exception("ZKBio access token is empty.");
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
                : throw new ZKBioConfigurationException(
                    $"{key} is not configured.");
    }
}
    
