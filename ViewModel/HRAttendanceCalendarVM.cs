namespace TSSC.Unified.ViewModel
{
    public class HRAttendanceCalendarVM
    {
        public int? EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public string? EmployeeCode { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string MonthName =>
            new DateTime(Year, Month, 1).ToString("MMMM yyyy");

        public List<AttendanceCalendarDayVM> Days { get; set; } = new();
    }

    public class AttendanceCalendarDayVM
    {
        public DateTime Date { get; set; }

        public bool IsCurrentMonth { get; set; }

        public string? Status { get; set; }

        public DateTime? InTime { get; set; }

        public DateTime? OutTime { get; set; }

        public double? WorkingHours { get; set; }

        public string? CheckInLocation { get; set; }

        public string? CheckOutLocation { get; set; }
    }
}
