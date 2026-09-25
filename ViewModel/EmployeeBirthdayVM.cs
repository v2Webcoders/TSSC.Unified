namespace TSSC.Unified.ViewModel
{
    public class EmployeeBirthdayVM
    {
        public string EmployeeName { get; set; }
        public DateTime DOB { get; set; }
        public string? PhotoPath { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsToday { get; set; }
    }

}
