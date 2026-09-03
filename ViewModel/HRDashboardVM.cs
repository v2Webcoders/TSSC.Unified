namespace TSSC.Unified.ViewModel
{
    public class HRDashboardVM
    {
        public string EmployeeName { get; set; } = "Employee";
        public string? EmployeeCode { get; set; }

        // Today's attendance for logged-in user
        public DateTime? TodayInTime { get; set; }
        public DateTime? TodayOutTime { get; set; }

        // HR KPIs
        public int TotalEmployees { get; set; }
        public int PresentToday { get; set; }
        public int EmployeesOnLeave { get; set; }
        public int PendingLeaveRequests { get; set; }

        // Recent leave requests
        public List<RecentLeaveRequestVM> RecentLeaveRequests { get; set; } = new();

        // Employee Dashboard
        public int PresentDaysThisMonth { get; set; }
        public int WorkingDaysThisMonth { get; set; }
        public int PendingRequests { get; set; }
        public int LeaveBalance { get; set; }
        public int AttendanceToRegularize { get; set; }
        public DateTime? DOB { get; set; }

        public List<EmployeeRecentRequestVM> RecentRequests { get; set; }
            = new();
        public List<EmployeeBirthdayVM>? Birthdays { get; set; } = new();
    }
    public class RecentLeaveRequestVM
    {
        public string EmployeeName { get; set; } = "";
        public string LeaveType { get; set; } = "";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Status { get; set; } = "";
    }
    public class EmployeeRecentRequestVM
    {
        public string RequestType { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = "";
    }
}
