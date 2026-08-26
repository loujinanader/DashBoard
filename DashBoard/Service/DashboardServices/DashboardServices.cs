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

        public DashboardService(IGLPIService glpiService,ITicketRepository ticketRepository)
        {
            _glpiService = glpiService;
            _ticketRepository = ticketRepository;
        }

        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var tickets = await _ticketRepository.GetAllAsync();

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
                    : new List<TeamMember>()
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
                    : new List<TeamMember>()
            };
        }
        public async Task<List<Ticket>> GetTicketsByUserIdAsync(int userId)
        {
            var tickets = await _ticketRepository.GetAllAsync();
            return tickets
                .Where(t =>!t.IsDeleted && t.AssignedUserId == userId)
                .Select(t => new Ticket
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
                    Team = new List<TeamMember>
                    {
                        new TeamMember
                        {
                            Id = t.AssignedUserId!.Value,
                            Name = t.AssignedUserName
                        }
                    }
                })
                .ToList();
        }

        public async Task<List<Ticket>> GetTicketsByStatusIdAsync(int statusId)
        {
            var tickets = await _ticketRepository.GetAllAsync();
            return tickets
                .Where(t =>!t.IsDeleted &&t.StatusId == statusId)
                .Select(t => new Ticket
                {
                    Id = t.Id,
                    Name = t.Name,
                    Status = new Status
                    {
                        Id = t.StatusId!.Value,
                        Name = t.StatusName
                    },
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
                        : new List<TeamMember>()
                })
                .ToList();
        }
        public async Task<DashboardSummary> GetTotalAsync()
            => await CreateSummary();
        public async Task<DashboardSummary> GetTotalByUserIdAsync(int userId)
        {
            var total = await _ticketRepository.GetCountByUserIdAsync(userId);
            var newTickets = await _ticketRepository.GetCountByStatusAndUserIdAsync( "New", userId);
            var processing = await _ticketRepository.GetCountByStatusAndUserIdAsync( "Processing", userId);
            var pending =await _ticketRepository.GetCountByStatusAndUserIdAsync("Pending", userId);
            var solved =await _ticketRepository.GetCountByStatusAndUserIdAsync( "Solved", userId);
            var closed = await _ticketRepository.GetCountByStatusAndUserIdAsync( "Closed", userId);
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
        private async Task<DashboardSummary> CreateSummary()
        {
            var total = await _ticketRepository.GetTotalAsync();
            var newTickets = await _ticketRepository.GetCountByStatusAsync("New");
            var processing = await _ticketRepository.GetCountByStatusAsync("Processing");
            var pending = await _ticketRepository.GetCountByStatusAsync("Pending");
            var solved = await _ticketRepository.GetCountByStatusAsync("Solved");
            var closed =await _ticketRepository.GetCountByStatusAsync("Closed");
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
        public async Task SyncTicketsAsync()
           => await _glpiService.SyncTicketsAsync();
    }
}



