namespace DashBoard.Models.Database
{
    public class AttendanceEntity
    {
        public int Id { get; set; }

        public string? EmployeeCode { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Department { get; set; }

        public string? Position { get; set; }

        public DateTime PunchTime { get; set; }

        public string? PunchState { get; set; }

        public string? PunchStateDisplay { get; set; }

        public int VerifyType { get; set; }

        public string? VerifyTypeDisplay { get; set; }

        public string? AreaAlias { get; set; }

        public string? TerminalSerialNumber { get; set; }

        public string? TerminalAlias { get; set; }
    }
}
