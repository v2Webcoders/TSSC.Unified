namespace TSSC.Unified.ViewModel
{

    public class CEODashboardVM
    {
        // =====================================================
        // CEO PROFILE
        // =====================================================

        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        // =====================================================
        // FINANCIAL YEAR
        // =====================================================

        public string FinancialYear { get; set; } = "2026-27";

        public List<string> FinancialYears { get; set; } = new();

        // =====================================================
        // 1. HR - WORKFORCE
        // =====================================================

        public CEOWorkforceVM Workforce { get; set; } = new();

        // =====================================================
        // 2. APPROVALS PENDING
        // =====================================================

        public CEOApprovalSummaryVM ApprovalSummary { get; set; } = new();

        public List<CEOApprovalRequestVM> PendingApprovals { get; set; }
            = new();

        // =====================================================
        // 3. TSSC KPIs
        // =====================================================

        public List<CEOKPIVM> FinancialKPIs { get; set; }
            = new();

        public List<CEOKPIVM> QualitativeKPIs { get; set; }
            = new();

        // =====================================================
        // 4. DEPARTMENT-WISE PORTAL ACCESS
        // =====================================================

        public List<CEODepartmentPortalVM> Departments { get; set; }
            = new();

        public List<string> ApprovalDepartments { get; set; } = new();
    }


    public class CEOWorkforceVM
    {
        public int TotalEmployees { get; set; }

        public int Present { get; set; }

        public int OnTime { get; set; }

        public int Delayed { get; set; }

        public decimal PresentPercentage { get; set; }

        public int OnDuty { get; set; }

        public int OnLeave { get; set; }

        public int Absent { get; set; }
    }


    public class CEOApprovalSummaryVM
    {
        public int PendingLeave { get; set; }

        public int AwaitingApprovals { get; set; }

        public int HighPriority { get; set; }

        public int Overdue { get; set; }
        public int Decided { get; set; }
    }

    public class CEOApprovalRequestVM
    {
        public long Id { get; set; }

        public string RequestId { get; set; } = string.Empty;

        public string RequestTitle { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string SubmittedBy { get; set; } = string.Empty;

        public DateTime SubmittedDate { get; set; }

        public DateTime? DueDate { get; set; }

        // Low, Medium, High
        public string Priority { get; set; } = "Low";

        // Pending, Approved, Rejected, Clarification
        public string Status { get; set; } = "Pending";

        public string? RequestType { get; set; }
    }

    public class CEOKPIVM
    {
        public string Title { get; set; } = string.Empty;

        // Main KPI value
        public decimal Value { get; set; }

        // Example: ₹ Cr, Students, %
        public string Unit { get; set; } = string.Empty;

        // Supporting information
        public string? ReceivedLabel { get; set; }

        public decimal? ReceivedValue { get; set; }

        public string? PendingLabel { get; set; }

        public decimal? PendingValue { get; set; }

        // Optional additional information
        public string? Description { get; set; }

        // Used for card styling
        public string? Color { get; set; }
    }

    public class CEODepartmentPortalVM
    {
        public int Id { get; set; }

        public string DepartmentName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Icon name for DashLite / Lucide
        public string? Icon { get; set; }

        // Department portal URL
        public string? PortalUrl { get; set; }

        public bool IsExternal { get; set; }
    }

}
