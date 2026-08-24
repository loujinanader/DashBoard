using System.Text.Json.Serialization;

namespace DashBoard.Models.Glpi
{
    public class Ticket
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Status? Status { get; set; }
        [JsonPropertyName("is_deleted")]
        public bool? is_delete { get; set; }
        public List<TeamMember>? Team { get; set; }
        
    }
}