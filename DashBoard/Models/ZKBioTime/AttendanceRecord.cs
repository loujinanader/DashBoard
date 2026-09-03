using System.Text.Json.Serialization;

namespace DashBoard.Models.ZKBioTime
{
    public class AttendanceRecord
    {
        public int Id { get; set; }

        [JsonPropertyName("emp_code")]
        public string? EmployeeCode { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        public string? Department { get; set; }

        public string? Position { get; set; }

        [JsonPropertyName("punch_time")]
        public DateTime PunchTime { get; set; }

        [JsonPropertyName("punch_state")]
        public string? PunchState { get; set; }

        [JsonPropertyName("punch_state_display")]
        public string? PunchStateDisplay { get; set; }

        [JsonPropertyName("verify_type")]
        public int VerifyType { get; set; }

        [JsonPropertyName("verify_type_display")]
        public string? VerifyTypeDisplay { get; set; }

        [JsonPropertyName("area_alias")]
        public string? AreaAlias { get; set; }

        [JsonPropertyName("terminal_sn")]
        public string? TerminalSerialNumber { get; set; }

        [JsonPropertyName("terminal_alias")]
        public string? TerminalAlias
        {
            get; set;
        }
    }
}
