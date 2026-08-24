namespace DashBoard.Models.Dashboard
{
    namespace DashBoard.Models
    {
        public class DashboardSummary
        {
            public int Total { get; set; }
            public int New { get; set; }
            public int Processing { get; set; }
            public int Pending { get; set; }
            public int Solved { get; set; }
            public int Closed { get; set; }
            
        }
    }
}
