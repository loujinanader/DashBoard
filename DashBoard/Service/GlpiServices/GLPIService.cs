using DashBoard.Brokers.ApiBroker.Glpi;
using DashBoard.Brokers.StorageBroker;
using DashBoard.Models.Database;
using DashBoard.Models.Glpi;
namespace DashBoard.Service.GlpiServices
{
    public class GLPIService : IGLPIService
    {
        private readonly IGLPIBroker _glpiBroker;
        private readonly IStorageBroker _storageBroker;
        public GLPIService(IGLPIBroker glpiBroker, IStorageBroker storageBroker)
        {
            _glpiBroker = glpiBroker;
            _storageBroker = storageBroker;
        }
        public async Task SyncTicketsAsync()
        {
            var tickets = await _glpiBroker.GetTicketsAsync();

            foreach (var ticket in tickets)
            {
                var entity = new TicketEntity
                {
                    Id = ticket.Id,
                    Name = ticket.Name,
                    StatusId = ticket.Status?.Id,
                    StatusName = ticket.Status?.Name,
                    IsDeleted = ticket.is_delete ?? false,
                    AssignedUserId = ticket.Team?.FirstOrDefault(member => member.Role == "assigned")?.Id,
                    AssignedUserName = ticket.Team?.FirstOrDefault(member => member.Role == "assigned")?.Name,
                    CreatedAt = ticket.DateCreation,
                    LocationId = ticket.Location.Id,
                    LocationName = ticket.Location.Name,
                    Type = ticket.Type
                };
                await _storageBroker.UpsertAsync(entity);
            }
            await _storageBroker.SaveChangesAsync();
        }
        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var tickets = await _glpiBroker.GetTicketsAsync();
            var validTickets = tickets.Where(t => t.is_delete != true).ToList();
            return validTickets;
        }
    }
}