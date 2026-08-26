namespace DashBoard.Models.Dashboard
{
    /// <summary>
    /// Not DashboardSummary: that type has no identity field because every existing
    /// caller already has exactly one implicit scope (the grand total, or the one
    /// userId in the URL). This one is a list — every row needs its own UserId/UserName
    /// to tell the rows apart — and it adds Other, which DashboardSummary's two existing
    /// consumers don't need and shouldn't have to carry.
    /// </summary>
    public class UserTicketSummary
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int Total { get; set; }
        public int New { get; set; }
        public int Processing { get; set; }
        public int Pending { get; set; }
        public int Solved { get; set; }
        public int Closed { get; set; }

        /// <summary>Tickets whose status falls outside the five tracked buckets above.</summary>
        public int Other => Total - New - Processing - Pending - Solved - Closed;
    }
}
