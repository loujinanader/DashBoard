using DashBoard.Brokers.StorageBroker;
using DashBoard.Models.Dashboard;
using DashBoard.Models.Dashboard.DashBoard.Models;
using DashBoard.Models.Glpi;
using DashBoard.Service.GlpiServices;
namespace DashBoard.Service.DashboardServices
{
    public class DashboardService : IDashboardServices
    {
        private readonly IGLPIService _glpiService;
        private readonly IStorageBroker _storageBroker;

        public DashboardService(IGLPIService glpiService, IStorageBroker storageBroker)
        {
            _glpiService = glpiService;
            _storageBroker = storageBroker;
        }

        public async Task<List<Ticket>> GetTicketsAsync(DateTime? from = null, DateTime? to = null)
        {
            var tickets = await _storageBroker.GetAllAsync(from, to);

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
                Location = new Location
                {
                    Id = t.LocationId,
                    Name = t.LocationName
                },
                Type = t.Type
            }).ToList();
        }
        public async Task<Ticket?> GetTicketByIdAsync(int id)
        {
            var ticket = await _storageBroker.GetByIdAsync(id);
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
                Location = new Location
                {
                    Id = ticket.LocationId,
                    Name = ticket.LocationName
                },
                Type = ticket.Type
            };
        }
        public async Task<List<Ticket>> GetTicketsByUserIdAsync(int userId)
        {
            var tickets = await _storageBroker.GetByUserIdAsync(userId);
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
                Location = new Location
                {
                    Id = t.LocationId,
                    Name = t.LocationName
                },
                Type = t.Type
            }).ToList();
        }

        public async Task<List<Ticket>> GetTicketsByStatusIdAsync(int statusId, DateTime? from = null, DateTime? to = null)
        {
            var tickets = await _storageBroker.GetByStatusIdAsync(statusId, from, to);
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
                Location = new Location
                {
                    Id = t.LocationId,
                    Name = t.LocationName
                },
                Type = t.Type
            }).ToList();
        }
        public async Task<DashboardSummary> GetTotalAsync(DateTime? from = null, DateTime? to = null) => await CreateSummary(from, to);
        public async Task<DashboardSummary> GetTotalByUserIdAsync(int userId)
        {
            var total = await _storageBroker.GetCountByUserIdAsync(userId);
            var newTickets = await _storageBroker.GetCountByStatusAndUserIdAsync("New", userId);
            var processing = await _storageBroker.GetCountByStatusAndUserIdAsync("Processing", userId);
            var pending = await _storageBroker.GetCountByStatusAndUserIdAsync("Pending", userId);
            var solved = await _storageBroker.GetCountByStatusAndUserIdAsync("Solved", userId);
            var closed = await _storageBroker.GetCountByStatusAndUserIdAsync("Closed", userId);
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
            var total = await _storageBroker.GetTotalAsync(from, to);
            var newTickets = await _storageBroker.GetCountByStatusAsync("New", from, to);
            var processing = await _storageBroker.GetCountByStatusAsync("Processing", from, to);
            var pending = await _storageBroker.GetCountByStatusAsync("Pending", from, to);
            var solved = await _storageBroker.GetCountByStatusAsync("Solved", from, to);
            var closed = await _storageBroker.GetCountByStatusAsync("Closed", from, to);
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
        public async Task<List<UserTicketSummary>> GetSummaryByAllUsersAsync(DateTime? from = null, DateTime? to = null) => await _storageBroker.GetSummaryByUserAsync(from, to);
        public async Task<List<LocationTicketSummary>> GetSummaryByAllLocationsAsync(DateTime? from = null, DateTime? to = null) => await _storageBroker.GetSummaryByLocationAsync(from, to);
        public async Task<TicketTypeSummary> GetSummaryByTypeAsync(DateTime? from = null, DateTime? to = null) => await _storageBroker.GetSummaryByTypeAsync(from, to);
        public async Task SyncTicketsAsync() => await _glpiService.SyncTicketsAsync();
    }
}



