namespace TSSC.Unified.ViewModel
{
    public class StandardsHeadDashboardVM
    {
        public string EmployeeName { get; set; } = "";
        public string? EmployeeCode { get; set; }
        public string Designation { get; set; } = "";

        // Attendance (today)
        public bool IsMarked { get; set; }
        public DateTime? InTime { get; set; }
        public DateTime? OutTime { get; set; }
        public TimeSpan? Worked { get; set; }
        public List<CEOBirthdayVM> TodaysBirthdays { get; set; } = new();
        public TeamWorkforceVM Team { get; set; } = new();

        public string FinancialYear { get; set; } = "";
        public List<string> FinancialYears { get; set; } = new();
        public StandardsApprovalSummaryVM ApprovalSummary { get; set; } = new();
        public List<CEOApprovalRequestVM> PendingApprovals { get; set; } = new();
        public List<string> ApprovalDepartments { get; set; } = new();
    }

    public class TeamWorkforceVM
    {
        public int Total { get; set; }
        public int OnTime { get; set; }
        public int Delayed { get; set; }
        public int Present => OnTime + Delayed;
        public int OnDuty { get; set; }
        public int OnLeave { get; set; }
        public int Absent { get; set; }
        public decimal PresentPercentage =>
            Total > 0 ? Math.Round((decimal)Present / Total * 100, 0) : 0;
    }
    public class StandardsApprovalSummaryVM
    {
        public int PendingLeave { get; set; }
        public int AwaitingApprovals { get; set; }
        public int HighPriority { get; set; }
        public int Overdue { get; set; }
    }

}
