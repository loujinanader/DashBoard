using DashBoard.Models.Glpi;
namespace DashBoard.Broker.Glpi
{
    public interface IGLPIBroker
    {
        public Task<string> GetAccessTokenAsync();
        public Task<List<Ticket>> GetTicketsAsync();
    }
}
