using DashBoard.Models.Glpi;
namespace DashBoard.ApiBroker.Glpi
{
    public interface IGlpiBroker
    {
       public Task<string> GetAccessTokenAsync();
       public Task<List<Ticket>> GetTicketsAsync();
    }
}
