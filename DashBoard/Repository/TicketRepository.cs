using DashBoard.Data;
using DashBoard.Models.Dashboard;
using DashBoard.Models.Database;
using Microsoft.EntityFrameworkCore;
namespace DashBoard.Repository
{
    public class TicketRepository : ITicketRepository
    {
        private readonly DashboardDbContext _context;
        private readonly DbSet<TicketEntity> _set;
        public TicketRepository(DashboardDbContext context)
        {
            _context = context;
            _set = _context.Set<TicketEntity>();
        }
        public async Task<List<TicketEntity>> GetAllAsync()
            => await _context.Tickets.Where(t => !t.IsDeleted).ToListAsync();
        public async Task<TicketEntity?> GetByIdAsync(int id)
           => await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        public async Task UpsertAsync(TicketEntity ticket)
        {
            var existingTicket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticket.Id);

            if (existingTicket == null)
                await _context.Tickets.AddAsync(ticket);
            else
            {
                existingTicket.Name = ticket.Name;
                existingTicket.StatusId = ticket.StatusId;
                existingTicket.StatusName = ticket.StatusName;
                existingTicket.IsDeleted = ticket.IsDeleted;
                existingTicket.AssignedUserId = ticket.AssignedUserId;
                existingTicket.AssignedUserName = ticket.AssignedUserName;
            }
        }
        public async Task SaveChangesAsync()
               => await _context.SaveChangesAsync();
        public async Task<int> GetTotalAsync()
               => await _context.Tickets.CountAsync(t => !t.IsDeleted);
        public async Task<int> GetCountByStatusAsync(string statusName)
               => await _context.Tickets.CountAsync(t => !t.IsDeleted && t.StatusName == statusName);
        public async Task<int> GetCountByUserIdAsync(int userId)
               => await _context.Tickets.CountAsync(t => !t.IsDeleted && t.AssignedUserId == userId);
        public async Task<int> GetCountByStatusAndUserIdAsync(string statusName, int userId)
                => await _context.Tickets.CountAsync(t => !t.IsDeleted && t.AssignedUserId == userId && t.StatusName == statusName);
        public async Task<List<TicketEntity>> GetByUserIdAsync(int userId)
            => await _context.Tickets.Where(t => !t.IsDeleted && t.AssignedUserId == userId).ToListAsync();
        public async Task<List<TicketEntity>> GetByStatusIdAsync(int statusId)
            => await _context.Tickets.Where(t => !t.IsDeleted && t.StatusId == statusId).ToListAsync();
        public async Task<List<UserTicketSummary>> GetSummaryByUserAsync()
        {
            var rows = await _context.Tickets
                .Where(t => !t.IsDeleted && t.AssignedUserId != null)
                .GroupBy(t => new { t.AssignedUserId, t.AssignedUserName })
                .Select(g => new UserTicketSummary
                {
                    UserId = g.Key.AssignedUserId!.Value,
                    UserName = g.Key.AssignedUserName,
                    Total = g.Count(),
                    New = g.Count(t => t.StatusName == "New"),
                    Processing = g.Count(t => t.StatusName == "Processing"),
                    Pending = g.Count(t => t.StatusName == "Pending"),
                    Solved = g.Count(t => t.StatusName == "Solved"),
                    Closed = g.Count(t => t.StatusName == "Closed")
                })
                .ToListAsync();
            return rows.OrderByDescending(r => r.Total).ToList();
        }
    }
}