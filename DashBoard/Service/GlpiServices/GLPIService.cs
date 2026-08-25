using DashBoard.Broker.Glpi;
using DashBoard.Models.Database;
using DashBoard.Models.Glpi;
using DashBoard.Repository;

namespace DashBoard.Service.GlpiServices
{
    public class GLPIService : IGLPIService
    {
        private readonly IGLPIBroker _glpiBroker;
        private readonly IConfiguration _configuration;
        private readonly ITicketRepository _ticketRepository;
        public GLPIService(
                IGLPIBroker glpiBroker,
            IConfiguration configuration,
            ITicketRepository ticketRepository)
        {
            _glpiBroker = glpiBroker;
            _configuration = configuration;
            _ticketRepository = ticketRepository;
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


                    AssignedUserId = ticket.Team?
                        .FirstOrDefault(member => member.Role == "assigned")
                        ?.Id,

                    AssignedUserName = ticket.Team?
                        .FirstOrDefault(member => member.Role == "assigned")
                        ?.Name
                };

                await _ticketRepository.UpsertAsync(entity);
            }

            await _ticketRepository.SaveChangesAsync();
        }
        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var tickets = await _glpiBroker.GetTicketsAsync();
            var validTickets = tickets
                .Where(t => t.is_delete != true)
                .ToList();
            return validTickets;
        }
    }
}