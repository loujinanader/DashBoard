namespace DashBoard.Models
{
    public class TicketTask
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public User? UserTech { get; set; }
        public User? GroupTech { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? PlannedBegin { get; set; }
        public DateTime? PlannedEnd { get; set; }
        public int State { get; set; }
        public int TicketsId { get; set; }
    }
}
