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
        private static IQueryable<TicketEntity> WithDateRange(IQueryable<TicketEntity> query, DateTime? from, DateTime? to)
        {
            if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value.Date);
            if (to.HasValue) query = query.Where(t => t.CreatedAt < to.Value.Date.AddDays(1));
            return query;
        }
        public async Task<List<TicketEntity>> GetAllAsync(DateTime? from = null, DateTime? to = null)
            => await WithDateRange(_context.Tickets.Where(t => !t.IsDeleted), from, to).ToListAsync();
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
                existingTicket.CreatedAt = ticket.CreatedAt;
                existingTicket.LocationId = ticket.LocationId;
                existingTicket.LocationName = ticket.LocationName;
            }
        }
        public async Task SaveChangesAsync()
               => await _context.SaveChangesAsync();
        public async Task<int> GetTotalAsync(DateTime? from = null, DateTime? to = null)
               => await WithDateRange(_context.Tickets.Where(t => !t.IsDeleted), from, to).CountAsync();
        public async Task<int> GetCountByStatusAsync(string statusName, DateTime? from = null, DateTime? to = null)
               => await WithDateRange(_context.Tickets.Where(t => !t.IsDeleted && t.StatusName == statusName), from, to).CountAsync();
        public async Task<int> GetCountByUserIdAsync(int userId)
               => await _context.Tickets.CountAsync(t => !t.IsDeleted && t.AssignedUserId == userId);
        public async Task<int> GetCountByStatusAndUserIdAsync(string statusName, int userId)
                => await _context.Tickets.CountAsync(t => !t.IsDeleted && t.AssignedUserId == userId && t.StatusName == statusName);
        public async Task<List<TicketEntity>> GetByUserIdAsync(int userId)
            => await _context.Tickets.Where(t => !t.IsDeleted && t.AssignedUserId == userId).ToListAsync();
        public async Task<List<TicketEntity>> GetByStatusIdAsync(int statusId, DateTime? from = null, DateTime? to = null)
            => await WithDateRange(_context.Tickets.Where(t => !t.IsDeleted && t.StatusId == statusId), from, to).ToListAsync();
        public async Task<List<UserTicketSummary>> GetSummaryByUserAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = WithDateRange(_context.Tickets.Where(t => !t.IsDeleted && t.AssignedUserId != null), from, to);
            var rows = await query
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
        public async Task<List<LocationTicketSummary>> GetSummaryByLocationAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = WithDateRange(_context.Tickets.Where(t => !t.IsDeleted), from, to);
            var rows = await query
                .GroupBy(t => new { t.LocationId, t.LocationName })
                .Select(g => new LocationTicketSummary
                {
                    LocationId = g.Key.LocationId,
                    LocationName = g.Key.LocationName,
                    Total = g.Count()
                })
                .ToListAsync();
            return rows.OrderByDescending(r => r.Total).ToList();
        }
    }
}