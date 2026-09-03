using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class ReimbursementsVM
    {
        public int ReimbursementId { get; set; }

        public int EmployeeId { get; set; }


        // =========================
        // REIMBURSEMENT DETAILS
        // =========================

        [Required(ErrorMessage = "Please select Request Type")]
        public string RequestType { get; set; }

        [Required(ErrorMessage = "Please select Expense Category")]
        public string ExpenseCategory { get; set; }

        [Required(ErrorMessage = "Please enter Expense Amount")]
        [Range(0.01, 999999999,
            ErrorMessage = "Please enter a valid amount")]
        public decimal ExpenseAmount { get; set; }

        [Required(ErrorMessage = "Please select Expense Date")]
        public DateTime ExpenseDate { get; set; }

        public string? VendorName { get; set; }

        [Required(ErrorMessage = "Please select Payment Mode")]
        public string PaymentMode { get; set; }

        [Required(ErrorMessage = "Please enter Purpose")]
        public string Purpose { get; set; }


        // =========================
        // FILE UPLOAD
        // =========================

        public IFormFile? SupportingDocumentFile { get; set; }

        public IFormFile? InvoiceBillFile { get; set; }

        public string? SupportingDocument { get; set; }

        public string? InvoiceBill { get; set; }


        // Employee Remarks
        public string? Remarks { get; set; }


        // Overall Status
        public string? Status { get; set; }


        // =========================
        // APPROVER 1
        // =========================

        public int? Approver1Id { get; set; }

        public string? Approver1Status { get; set; }

        public string? Approver1Remarks { get; set; }

        public DateTime? Approver1ApprovedOn { get; set; }


        // =========================
        // APPROVER 2
        // =========================

        public int? Approver2Id { get; set; }

        public string? Approver2Status { get; set; }

        public string? Approver2Remarks { get; set; }

        public DateTime? Approver2ApprovedOn { get; set; }


        // =========================
        // APPROVER 3
        // =========================

        public int? Approver3Id { get; set; }

        public string? Approver3Status { get; set; }

        public string? Approver3Remarks { get; set; }

        public DateTime? Approver3ApprovedOn { get; set; }


        // =========================
        // APPROVER 4
        // =========================

        public int? Approver4Id { get; set; }

        public string? Approver4Status { get; set; }

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
        // FINANCE
        // =========================

        public string? FinalStatus { get; set; }

        public string? FinalRemarks { get; set; }

        public DateTime? FinalApprovedOn { get; set; }


        // Payment
        public DateTime? PaidOn { get; set; }


        // System Fields
        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; }


        // =========================
        // DROPDOWNS
        // =========================

        public List<SelectListItem>? RequestTypeList { get; set; }

        public List<SelectListItem>? ExpenseCategoryList { get; set; }

        public List<SelectListItem>? PaymentModeList { get; set; }




        // Employee / Approver dropdown
        public List<SelectListItem>? ApproverList { get; set; }
        public string? Approver1Name { get; set; }
        public string? Approver2Name { get; set; }
        public string? Approver3Name { get; set; }
        public string? Approver4Name { get; set; }
        public string? Approver5Name { get; set; }
        public string? Approver6Name { get; set; }
    }
}