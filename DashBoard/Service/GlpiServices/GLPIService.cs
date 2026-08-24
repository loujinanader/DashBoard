using DashBoard.ApiBroker.Glpi;
using DashBoard.Models.Glpi;
using System.Net.Http.Headers;
using System.Text.Json;
namespace DashBoard.Service.GlpiServices
{
    public class GLPIService : IGLPIService
    {
        private readonly IGlpiBroker _glpiBroker;
        private readonly IConfiguration _configuration;

        public GLPIService(
                IGlpiBroker glpiBroker,
            IConfiguration configuration)
        {
            _glpiBroker = glpiBroker;
            _configuration = configuration;
        }

        public async Task<List<Ticket>> GetTicketsAsync()
        {
            var tickets =
                await _glpiBroker.GetTicketsAsync();

            var itUserIds =
                _configuration
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

            return itTickets;
        }
    }
}