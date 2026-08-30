namespace DashBoard.Models.Database
{
    public class TicketEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? StatusId { get; set; }
        public string? StatusName { get; set; }
        public bool IsDeleted { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public int Type { get; set; }
    }
}