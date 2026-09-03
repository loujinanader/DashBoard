using DashBoard.Models.Glpi;
namespace DashBoard.Brokers.ApiBroker.Glpi
{
    public interface IGLPIBroker
    {
        public Task<string> GetAccessTokenAsync();
        public Task<List<Ticket>> GetTicketsAsync();
    }
}
