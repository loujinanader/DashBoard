using DashBoard.Broker.Glpi;
using DashBoard.Models.Glpi;
namespace DashBoard.Service.GlpiServices
{
    public class GLPIService : IGLPIService
    {
        private readonly IGLPIBroker _glpiBroker;
        private readonly IConfiguration _configuration;

        public GLPIService(
                IGLPIBroker glpiBroker,
            IConfiguration configuration)
        {
            _glpiBroker = glpiBroker;
            _configuration = configuration;
        }

        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var tickets = await _glpiBroker.GetTicketsAsync();
            var itUserIds = _configuration
                    .GetSection("GLPI:ITUserIds")
                    .Get<int[]>()
                ?? Array.Empty<int>();
            var itTickets =
                tickets
                    .Where(t =>
                        t.Team != null &&
                        t.Team.Any(member =>
                        itUserIds.Contains(member.Id)))
                    .ToList();

            return tickets;
        }

    }
}