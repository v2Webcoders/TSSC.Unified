namespace TSSC.Unified.ViewModel
{
    public class LeaveAdjustmentVM
    {
        public int EmployeeId { get; set; }

        public int LeaveTypeId { get; set; }

        public string AdjustmentType { get; set; }

        public decimal Days { get; set; }

        public string Reason { get; set; }

        public decimal CurrentBalance { get; set; }
    }
}
