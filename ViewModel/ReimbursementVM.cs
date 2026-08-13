using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class ReimbursementVM
    {
        public int ReimbursementId { get; set; }

        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Please select Request Type")]
        public string RequestType { get; set; }

        [Required(ErrorMessage = "Please select Expense Category")]
        public string ExpenseCategory { get; set; }

        [Required(ErrorMessage = "Please enter Expense Amount")]
        [Range(0.01, 999999999, ErrorMessage = "Please enter a valid amount")]
        public decimal ExpenseAmount { get; set; }

        [Required(ErrorMessage = "Please select Expense Date")]
        public DateTime ExpenseDate { get; set; }

        public string VendorName { get; set; }

        [Required(ErrorMessage = "Please select Payment Mode")]
        public string PaymentMode { get; set; }

        [Required(ErrorMessage = "Please enter Purpose")]
        public string Purpose { get; set; }

        // File upload fields
        public IFormFile SupportingDocumentFile { get; set; }
        public IFormFile InvoiceBillFile { get; set; }

        // These will store file paths - NOT REQUIRED in form
        public string SupportingDocument { get; set; }
        public string InvoiceBill { get; set; }

        public string Remarks { get; set; }

        // System fields - will be set in controller
        public string Status { get; set; }
        public int? ReportingManagerId { get; set; }
        public string ManagerRemarks { get; set; }
        public DateTime? ManagerApprovedOn { get; set; }
        public string? FinanceStatus { get; set; }
        public string FinanceRemarks { get; set; }
        public DateTime? FinanceApprovedOn { get; set; }
        public DateTime? PaidOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsActive { get; set; }

        // Dropdowns
        public List<SelectListItem> RequestTypeList { get; set; }
        public List<SelectListItem> ExpenseCategoryList { get; set; }
        public List<SelectListItem> PaymentModeList { get; set; }
    }

    public class MyReimbursementVM
    {
        public List<ReimbursementVM> ReimbursementList { get; set; }
    }
}