namespace TSSC.Unified.Models
{

    public class CEOApproval
    {
        public long ApprovalId { get; set; }

        public string RequestId { get; set; } = string.Empty;

        public string RequestType { get; set; } = string.Empty;

        public string RequestTitle { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string SubmittedBy { get; set; } = string.Empty;

        public DateTime SubmittedDate { get; set; }

        public DateTime? DueDate { get; set; }

        public string Priority { get; set; } = "Low";

        public string Status { get; set; } = "Pending";

        public string? CurrentStage { get; set; }

        public bool IsCEOApproval { get; set; }

        public DateTime? DecisionDate { get; set; }

        public string? ApprovalRemarks { get; set; }

        public string SourceTable { get; set; } = string.Empty;

        public int? RequesterEmployeeId { get; set; }

        public int? CEOEmployeeId { get; set; }

        public int? ApprovalStep { get; set; }

        public string FinancialYear { get; set; } = string.Empty;
    }


}
