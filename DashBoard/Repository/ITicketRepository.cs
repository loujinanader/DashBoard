using DashBoard.Models.Database;

namespace DashBoard.Repository
{
    public interface ITicketRepository
    {
        public Task<List<TicketEntity>> GetAllAsync();
        public Task<TicketEntity?> GetByIdAsync(int id);
        public Task UpsertAsync(TicketEntity ticket);
        public Task SaveChangesAsync();
    }
}
