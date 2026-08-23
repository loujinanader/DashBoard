using DashBoard.Models.Glpi;
namespace DashBoard.Service
{
    public interface IGLPIService
    {
        public Task<List<Ticket>> GetTicketsAsync();
    }
}
