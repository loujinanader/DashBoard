using DashBoard.Models.Glpi;

namespace DashBoard.Brokers.ApiBroker.ZKBio
{
    public interface IZKBioBroker
    {
        public Task<string> GetAccessTokenAsync();
        public Task<List<Ticket>> GetFingerPrintsAsync();
    }
}
