using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TSSC.Unified.ViewModel
{
    public class LeaveRequestVM
    {
        public int LeaveRequestId { get; set; }

        // Employee Details
        public int EmployeeId { get; set; }

        [ValidateNever]
        public string EmployeeName { get; set; }

        [ValidateNever]
        public string DepartmentName { get; set; }

        // Leave Details

        [Required(ErrorMessage = "Please select leave type")]
        public int LeaveTypeId { get; set; }

        [Required(ErrorMessage = "Please select leave duration")]
        public string LeaveDuration { get; set; }

        [Required(ErrorMessage = "Please select from date")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "Please select to date")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }

        [Required(ErrorMessage = "Please enter leave reason")]
        [StringLength(500)]
        public string Reason { get; set; }

        // Attachment

        public string? Attachment { get; set; }

        public IFormFile? AttachmentFile { get; set; }

        // Contact During Leave

        [StringLength(15)]
        public string? EmergencyContact { get; set; }

        // Approval Status

        public string? Status { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public string? ApprovalRemarks { get; set; }

        // Dropdown Display

        public string? LeaveTypeName { get; set; }
    }
}
