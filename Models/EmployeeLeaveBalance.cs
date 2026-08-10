namespace TSSC.Unified.Models
{
    public class EmployeeLeaveBalance
    {
        public int EmployeeLeaveBalanceId { get; set; }

        public int EmployeeId { get; set; }

        public int LeaveTypeId { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal UsedLeaves { get; set; }

        public decimal Adjustment { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public Employee Employee { get; set; }

        public LeaveType LeaveType { get; set; }

        public decimal CurrentBalance =>
            OpeningBalance + Adjustment - UsedLeaves;
    }
}
