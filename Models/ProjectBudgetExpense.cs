using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class ProjectBudgetExpense
    {
        [Key]
        public int ExpenseId { get; set; }

        public int BudgetId { get; set; }

        public int ProjectId { get; set; }

        [Required]
        [StringLength(200)]
        public string ExpenseTitle { get; set; }

        public decimal ExpenseAmount { get; set; }

        public DateTime ExpenseDate { get; set; }

        public int? VendorId { get; set; }

        public string? PaymentMode { get; set; }

        public string? InvoiceNumber { get; set; }

        public string? Remarks { get; set; }

        public string? InvoiceAttachment { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        // LEVEL 1: Business Head Approval

        public string BusinessHeadStatus { get; set; } = "Pending";

        public string? BusinessHeadRemarks { get; set; }

        public int? BusinessHeadReviewedBy { get; set; }

        public DateTime? BusinessHeadReviewedDate { get; set; }


        // LEVEL 2: CEO Approval

        public string CEOStatus { get; set; } = "Pending";

        public string? CEORemarks { get; set; }

        public int? CEOReviewedBy { get; set; }

        public DateTime? CEOReviewedDate { get; set; }
    }
}
