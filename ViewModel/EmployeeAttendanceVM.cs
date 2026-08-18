namespace TSSC.Unified.ViewModel
{
    public class EmployeeAttendanceVM
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public DateTime AttendanceDate { get; set; }

        public DateTime? InTime { get; set; }

        public DateTime? OutTime { get; set; }

        public string AttendanceStatus { get; set; }

        public string AttendanceSource { get; set; }

        public bool IsCheckedIn =>
            InTime.HasValue;

        public bool IsCheckedOut =>
            OutTime.HasValue;
    }
}
