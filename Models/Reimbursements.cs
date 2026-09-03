using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class Reimbursements
    {
        [Key]
        public int ReimbursementId { get; set; }

        // Employee Details
        public int EmployeeId { get; set; }


        // Reimbursement Details
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
        public string? VendorName { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMode { get; set; }

        [Required]
        [StringLength(1000)]
        public string Purpose { get; set; }


        // Documents
        [StringLength(500)]
        public string? SupportingDocument { get; set; }

        [StringLength(500)]
        public string? InvoiceBill { get; set; }


        // Employee Remarks
        [StringLength(1000)]
        public string? Remarks { get; set; }


        // Overall Status
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";


        // =========================
        // APPROVER 1
        // =========================

        public int? Approver1Id { get; set; }

        [StringLength(50)]
        public string? Approver1Status { get; set; } = "Pending";

        [StringLength(1000)]
        public string? Approver1Remarks { get; set; }

        public DateTime? Approver1ApprovedOn { get; set; }


        // =========================
        // APPROVER 2
        // =========================

        public int? Approver2Id { get; set; }

        [StringLength(50)]
        public string? Approver2Status { get; set; } = "Pending";

        [StringLength(1000)]
        public string? Approver2Remarks { get; set; }

        public DateTime? Approver2ApprovedOn { get; set; }


        // =========================
        // APPROVER 3
        // =========================

        public int? Approver3Id { get; set; }

        [StringLength(50)]
        public string? Approver3Status { get; set; } = "Pending";

        [StringLength(1000)]
        public string? Approver3Remarks { get; set; }

        public DateTime? Approver3ApprovedOn { get; set; }


        // =========================
        // APPROVER 4
        // =========================

        public int? Approver4Id { get; set; }

        [StringLength(50)]
        public string? Approver4Status { get; set; } = "Pending";

        [StringLength(1000)]
        public string? Approver4Remarks { get; set; }

        public DateTime? Approver4ApprovedOn { get; set; }
        // =========================
        // APPROVER 5
        // =========================

        public int? Approver5Id { get; set; }

        [StringLength(50)]
        public string? Approver5Status { get; set; } = "Pending";

        [StringLength(1000)]
        public string? Approver5Remarks { get; set; }

        public DateTime? Approver5ApprovedOn { get; set; }
        // =========================
        // APPROVER 6
        // =========================

        public int? Approver6Id { get; set; }

        [StringLength(50)]
        public string? Approver6Status { get; set; } = "Pending";

        [StringLength(1000)]
        public string? Approver6Remarks { get; set; }

        public DateTime? Approver6ApprovedOn { get; set; }


        // =========================
        // FINANCE APPROVAL
        // =========================

        [Required]
        [StringLength(50)]
        public string FinalStatus { get; set; } = "Pending";

        [StringLength(1000)]
        public string? FinalRemarks { get; set; }

        public DateTime? FinalApprovedOn { get; set; }


        // Payment
        public DateTime? PaidOn { get; set; }


        // System Fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;
    }
}