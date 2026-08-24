using DashBoard.Models.Dashboard.DashBoard.Models;
using DashBoard.Models.Glpi;
using DashBoard.Service.GlpiServices;
namespace DashBoard.Service.DashboardServices
{
    public class DashboardService : IDashboardServices
    {
        private readonly IGLPIService _glpiService;
        public DashboardService(IGLPIService glpiService)
        {
            _glpiService = glpiService;
        }
        public async Task<List<Ticket>> GetTicketsAsync()
            => await _glpiService.GetTicketsAsync();
        public async Task<Ticket?> GetTicketByIdAsync(int id)
        {
            var tickets = await _glpiService.GetTicketsAsync();

            return tickets.FirstOrDefault(t => t.Id == id);
        }
        public async Task<List<Ticket>> GetTicketsByUserIdAsync(int userId)
        {
            var tickets = await _glpiService.GetTicketsAsync();
            return tickets
                .Where(t => t.Team != null &&
                            t.Team.Any(member => member.Id == userId))
                .ToList();
        }
        public async Task<List<Ticket>> GetTicketsByStatusIdAsync(int statusId)
        {
            var tickets = await _glpiService.GetTicketsAsync();
            return tickets
                .Where(t => t.Status?.Id == statusId)
                .ToList();
        }
        public async Task<DashboardSummary> GetTotalAsync()
        {
            var tickets = await _glpiService.GetTicketsAsync();
            return CreateSummary(tickets);
        }
        public async Task<DashboardSummary> GetTotalByUserIdAsync(int userId)
        {
            var tickets = await GetTicketsByUserIdAsync(userId);
            return CreateSummary(tickets);
        }
        private DashboardSummary CreateSummary(List<Ticket> tickets)
        {
            return new DashboardSummary
            {
                Total = tickets.Count,
                New = tickets.Count(t => t.Status?.Id == 1),
                Processing = tickets.Count(t => t.Status?.Id == 2),
                Pending = tickets.Count(t => t.Status?.Id == 4),
                Solved = tickets.Count(t => t.Status?.Id == 5),
                Closed = tickets.Count(t => t.Status?.Id == 6)
            };
        }
    }

}

