using DashBoard.Models.Database;

namespace DashBoard.Repository
{
    public interface ITicketRepository
    {
        public Task<List<TicketEntity>> GetAllAsync();
        public Task<TicketEntity?> GetByIdAsync(int id);
        public Task UpsertAsync(TicketEntity ticket);
        public Task SaveChangesAsync();
       public Task<int> GetTotalAsync();
       public Task<int> GetCountByStatusAsync(string statusName);
        public Task<int> GetCountByUserIdAsync(int userId);
        public Task<int> GetCountByStatusAndUserIdAsync(string statusName, int userId);
    }
}
