using DashBoard.Models.Dashboard;
using DashBoard.Models.Dashboard.DashBoard.Models;
using DashBoard.Models.Glpi;
namespace DashBoard.Service.DashboardServices
{
    public interface IDashboardServices
    {
        public Task<List<Ticket>> GetTicketsAsync(DateTime? from = null, DateTime? to = null);
        public Task<Ticket?> GetTicketByIdAsync(int id);
        public Task<List<Ticket>> GetTicketsByUserIdAsync(int userId);
        public Task<List<Ticket>> GetTicketsByStatusIdAsync(int statusId, DateTime? from = null, DateTime? to = null);
        public Task<DashboardSummary> GetTotalAsync(DateTime? from = null, DateTime? to = null);
        public Task<DashboardSummary> GetTotalByUserIdAsync(int userId);
        public Task<List<UserTicketSummary>> GetSummaryByAllUsersAsync(DateTime? from = null, DateTime? to = null);
        public Task<List<LocationTicketSummary>> GetSummaryByAllLocationsAsync(DateTime? from = null, DateTime? to = null);
        public Task SyncTicketsAsync();
    }
}
