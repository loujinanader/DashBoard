using DashBoard.Models.Glpi;

namespace DashBoard.Service
{
    public interface IGLPIService
    {
        public Task<List<Ticket>> GetTicketsAsync();

        public string GetAuthorizationUrl();

        public Task ExchangeAuthorizationCodeAsync(string code);
    }
}
