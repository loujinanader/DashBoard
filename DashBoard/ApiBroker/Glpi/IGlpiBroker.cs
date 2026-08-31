using DashBoard.Models.Glpi;
namespace DashBoard.ApiBroker.Glpi
{
    public interface IGLPIBroker
    {
        public Task<string> GetAccessTokenAsync();
        public Task<List<Ticket>> GetTicketsAsync();
    }
}
