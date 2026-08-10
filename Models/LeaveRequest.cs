namespace TSSC.Unified.Models
{
    public class LeaveRequest
    {
        public int LeaveRequestId { get; set; }

        public int EmployeeId { get; set; }

        public int LeaveTypeId { get; set; }

        public string LeaveDuration { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal TotalDays { get; set; }

        public string Reason { get; set; }

        public string? Attachment { get; set; }

        public string? EmergencyContact { get; set; }


        // Workflow

        public string Status { get; set; } = "Pending";

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public string? ApprovalRemarks { get; set; }


        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? ApproverId { get; set; }      // Reporting Manager
        public LeaveType LeaveType { get; set; }   // <-- Required
    }
}
