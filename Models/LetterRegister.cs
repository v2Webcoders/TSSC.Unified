using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class LetterRegister
    {
        [Key]
        public int LetterId { get; set; }

        [Required]
        public string LetterType { get; set; }

        [Required]
        public string ItemName { get; set; }

        public string? SenderName { get; set; }

        public string? ReceiverName { get; set; }

        public string? Mode { get; set; }

        public string? DocketNo { get; set; }

        public int? ReceivedBy { get; set; }

        public int? SentBy { get; set; }

        public int? DelegatedBy { get; set; }

        public int? DelegatedTo { get; set; }

        public int? Handover { get; set; }

        public DateTime? HandoverDate { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; }

        public bool IsActive { get; set; }
        public string? LetterAttachment { get; set; }

        // Received / Not Received
        public string EmployeeReceiveStatus { get; set; } = "Not Received";

        public DateTime? EmployeeReceiveDate { get; set; }
    }
}
