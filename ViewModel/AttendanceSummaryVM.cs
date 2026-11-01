namespace TSSC.Unified.ViewModel
{
    public class AttendanceSummaryVM
    {
        public int EmployeeId { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int PresentCount { get; set; }

        public int LateCount { get; set; }

        public int HalfDayCount { get; set; }

        public int AbsentCount { get; set; }

        public decimal LeaveCount { get; set; }

        public int HolidayCount { get; set; }

        public int MissingPunchCount { get; set; }

        public decimal LateLeaveDeduction { get; set; }
        public int WFHCount { get; set; }
        public decimal DaysWorked { get; set; }
    }
}
