using DashBoard.Models.Dashboard;
using DashBoard.Models.Database;
namespace DashBoard.Repository
{
    public interface ITicketRepository
    {
        public Task<List<TicketEntity>> GetAllAsync(DateTime? from = null, DateTime? to = null);
        public Task<TicketEntity?> GetByIdAsync(int id);
        public Task UpsertAsync(TicketEntity ticket);
        public Task SaveChangesAsync();
        public Task<int> GetTotalAsync(DateTime? from = null, DateTime? to = null);
        public Task<int> GetCountByStatusAsync(string statusName, DateTime? from = null, DateTime? to = null);
        public Task<int> GetCountByUserIdAsync(int userId);
        public Task<int> GetCountByStatusAndUserIdAsync(string statusName, int userId);
        public Task<List<TicketEntity>> GetByUserIdAsync(int userId);
        public Task<List<TicketEntity>> GetByStatusIdAsync(int statusId, DateTime? from = null, DateTime? to = null);
        public Task<List<UserTicketSummary>> GetSummaryByUserAsync(DateTime? from = null, DateTime? to = null);
    }
}
