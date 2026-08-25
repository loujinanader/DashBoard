using DashBoard.Models.Glpi;
namespace DashBoard.Service.GlpiServices
{
    public interface IGLPIService
    {
        public Task<List<Ticket>> GetTicketsAsync();
        public Task SyncTicketsAsync();

    }
}
