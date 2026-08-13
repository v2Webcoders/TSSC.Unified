using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TSSC.Unified.Models

{


public class Reimbursement
    {
         [Key]
        public int ReimbursementId { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string RequestType { get; set; }

        [Required]
        [StringLength(100)]
        public string ExpenseCategory { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpenseAmount { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; }

        [StringLength(200)]
        public string VendorName { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMode { get; set; }

        [Required]
        [StringLength(1000)]
        public string Purpose { get; set; }

        [StringLength(500)]
        public string SupportingDocument { get; set; }

        [StringLength(500)]
        public string InvoiceBill { get; set; }

        [StringLength(1000)]
        public string Remarks { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public int? ReportingManagerId { get; set; }

        [StringLength(1000)]
        public string? ManagerRemarks { get; set; }

        public DateTime? ManagerApprovedOn { get; set; }
        public string? FinanceStatus { get; set; }

        [StringLength(1000)]
        public string? FinanceRemarks { get; set; }

        public DateTime? FinanceApprovedOn { get; set; }

        public DateTime? PaidOn { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; }
    }
}
