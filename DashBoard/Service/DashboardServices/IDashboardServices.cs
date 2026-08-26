using DashBoard.Models.Dashboard;
using DashBoard.Models.Dashboard.DashBoard.Models;
using DashBoard.Models.Glpi;
namespace DashBoard.Service.DashboardServices
{
    public interface IDashboardServices
    {
        public Task<List<Ticket>> GetTicketsAsync();
        public Task<Ticket?> GetTicketByIdAsync(int id);
        public Task<List<Ticket>> GetTicketsByUserIdAsync(int userId);
        public Task<List<Ticket>> GetTicketsByStatusIdAsync(int statusId);
        public Task<DashboardSummary> GetTotalAsync();
        public Task<DashboardSummary> GetTotalByUserIdAsync(int userId);
        public Task<List<UserTicketSummary>> GetSummaryByAllUsersAsync();
        public Task SyncTicketsAsync();
    }
}
