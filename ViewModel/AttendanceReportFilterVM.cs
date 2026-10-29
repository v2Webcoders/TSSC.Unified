namespace TSSC.Unified.ViewModel
{
    public class AttendanceReportFilterVM
    {
        public int Month { get; set; } = DateTime.Today.Month;

        public int Year { get; set; } = DateTime.Today.Year;
    }
}
