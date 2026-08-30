using DashBoard.Models.Dashboard;
using DashBoard.Models.Dashboard.DashBoard.Models;
using DashBoard.Models.Glpi;
using DashBoard.Repository;
using DashBoard.Service.GlpiServices;
namespace DashBoard.Service.DashboardServices
{
    public class DashboardService : IDashboardServices
    {
        private readonly IGLPIService _glpiService;
        private readonly ITicketRepository _ticketRepository;

        public DashboardService(IGLPIService glpiService, ITicketRepository ticketRepository)
        {
            _glpiService = glpiService;
            _ticketRepository = ticketRepository;
        }

        public async Task<List<Ticket>> GetTicketsAsync(DateTime? from = null, DateTime? to = null)
        {
            var tickets = await _ticketRepository.GetAllAsync(from, to);

            return tickets.Select(t => new Ticket
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.StatusId.HasValue
                    ? new Status
                    {
                        Id = t.StatusId.Value,
                        Name = t.StatusName
                    }
                    : null,
                is_delete = t.IsDeleted,
                Team = t.AssignedUserId.HasValue
                    ? new List<TeamMember>
                    {
                        new TeamMember
                        {
                            Id = t.AssignedUserId.Value,
                            Name = t.AssignedUserName
                        }
                    }
                    : new List<TeamMember>(),
                DateCreation = t.CreatedAt,
                Location = t.LocationId.HasValue
                    ? new Location
                    {
                        Id = t.LocationId.Value,
                        Name = t.LocationName
                    }
                    : null
            }).ToList();
        }
        public async Task<Ticket?> GetTicketByIdAsync(int id)
        {
            var ticket = await _ticketRepository.GetByIdAsync(id);
            if (ticket == null)
                return null;

            return new Ticket
            {
                Id = ticket.Id,
                Name = ticket.Name,
                Status = ticket.StatusId.HasValue
                    ? new Status
                    {
                        Id = ticket.StatusId.Value,
                        Name = ticket.StatusName
                    }
                    : null,
                is_delete = ticket.IsDeleted,
                Team = ticket.AssignedUserId.HasValue
                    ? new List<TeamMember>
                    {
                        new TeamMember
                        {
                            Id = ticket.AssignedUserId.Value,
                            Name = ticket.AssignedUserName
                        }
                    }
                    : new List<TeamMember>(),
                DateCreation = ticket.CreatedAt,
                Location = ticket.LocationId.HasValue
                    ? new Location
                    {
                        Id = ticket.LocationId.Value,
                        Name = ticket.LocationName
                    }
                    : null
            };
        }
        public async Task<List<Ticket>> GetTicketsByUserIdAsync(int userId)
        {
            var tickets = await _ticketRepository.GetByUserIdAsync(userId);
            return tickets.Select(t => new Ticket
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.StatusId.HasValue
                    ? new Status
                    {
                        Id = t.StatusId.Value,
                        Name = t.StatusName
                    } : null,
                is_delete = t.IsDeleted,
                Team = t.AssignedUserId.HasValue
                    ? new List<TeamMember>
                    {
                new TeamMember
                {
                    Id = t.AssignedUserId.Value,
                    Name = t.AssignedUserName
                }
                    } : new List<TeamMember>(),
                DateCreation = t.CreatedAt,
                Location = t.LocationId.HasValue
                    ? new Location
                    {
                        Id = t.LocationId.Value,
                        Name = t.LocationName
                    }
                    : null
            }).ToList();
        }

        public async Task<List<Ticket>> GetTicketsByStatusIdAsync(int statusId, DateTime? from = null, DateTime? to = null)
        {
            var tickets = await _ticketRepository.GetByStatusIdAsync(statusId, from, to);
            return tickets.Select(t => new Ticket
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.StatusId.HasValue
                    ? new Status
                    {
                        Id = t.StatusId.Value,
                        Name = t.StatusName
                    }
                    : null,
                is_delete = t.IsDeleted,
                Team = t.AssignedUserId.HasValue
                    ? new List<TeamMember>
                    {
                new TeamMember
                {
                    Id = t.AssignedUserId.Value,
                    Name = t.AssignedUserName
                }
                    }
                    : new List<TeamMember>(),
                DateCreation = t.CreatedAt,
                Location = t.LocationId.HasValue
                    ? new Location
                    {
                        Id = t.LocationId.Value,
                        Name = t.LocationName
                    }
                    : null
            }).ToList();
        }
        public async Task<DashboardSummary> GetTotalAsync(DateTime? from = null, DateTime? to = null) => await CreateSummary(from, to);
        public async Task<DashboardSummary> GetTotalByUserIdAsync(int userId)
        {
            var total = await _ticketRepository.GetCountByUserIdAsync(userId);
            var newTickets = await _ticketRepository.GetCountByStatusAndUserIdAsync("New", userId);
            var processing = await _ticketRepository.GetCountByStatusAndUserIdAsync("Processing", userId);
            var pending = await _ticketRepository.GetCountByStatusAndUserIdAsync("Pending", userId);
            var solved = await _ticketRepository.GetCountByStatusAndUserIdAsync("Solved", userId);
            var closed = await _ticketRepository.GetCountByStatusAndUserIdAsync("Closed", userId);
            return new DashboardSummary
            {
                Total = total,
                New = newTickets,
                Processing = processing,
                Pending = pending,
                Solved = solved,
                Closed = closed
            };
        }
        private async Task<DashboardSummary> CreateSummary(DateTime? from = null, DateTime? to = null)
        {
            var total = await _ticketRepository.GetTotalAsync(from, to);
            var newTickets = await _ticketRepository.GetCountByStatusAsync("New", from, to);
            var processing = await _ticketRepository.GetCountByStatusAsync("Processing", from, to);
            var pending = await _ticketRepository.GetCountByStatusAsync("Pending", from, to);
            var solved = await _ticketRepository.GetCountByStatusAsync("Solved", from, to);
            var closed = await _ticketRepository.GetCountByStatusAsync("Closed", from, to);
            return new DashboardSummary
            {
                Total = total,
                New = newTickets,
                Processing = processing,
                Pending = pending,
                Solved = solved,
                Closed = closed
            };
        }
        public async Task<List<UserTicketSummary>> GetSummaryByAllUsersAsync(DateTime? from = null, DateTime? to = null) => await _ticketRepository.GetSummaryByUserAsync(from, to);
        public async Task SyncTicketsAsync() => await _glpiService.SyncTicketsAsync();
    }
}



