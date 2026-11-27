namespace TSSC.Unified.Models
{
    public class StandardsHeadApproval
    {
        public long ApprovalId { get; set; }
        public string RequestId { get; set; } = "";
        public string RequestType { get; set; } = "";
        public string RequestTitle { get; set; } = "";
        public string? Department { get; set; }
        public string? SubmittedBy { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "";
        public string Status { get; set; } = "";
        public string? CurrentStage { get; set; }
        public DateTime? DecisionDate { get; set; }
        public string? ApprovalRemarks { get; set; }
        public string SourceTable { get; set; } = "";
        public int? RequesterEmployeeId { get; set; }
        public int? ApproverEmployeeId { get; set; }
        public int? ApprovalStep { get; set; }
        public string FinancialYear { get; set; } = "";
    }
}
