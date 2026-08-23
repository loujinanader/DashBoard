namespace DashBoard.Models.Glpi
{
    public class Ticket
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Status? Status { get; set; }
        public List<TeamMember>? Team { get; set; }
    }
}