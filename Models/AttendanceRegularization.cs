using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class AttendanceRegularization
    {
        [Key]
        public int RegularizationId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        // Existing biometric attendance
        public DateTime? ExistingInTime { get; set; }

        public DateTime? ExistingOutTime { get; set; }

        // Employee requested correction
        public DateTime? RequestedInTime { get; set; }

        public DateTime? RequestedOutTime { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        [StringLength(500)]
        public string? Attachment { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        // Reporting Manager
        public int? ApproverId { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        public string? ApprovalRemarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; }

        [ForeignKey(nameof(ApproverId))]
        public virtual Employee? Approver { get; set; }
    }
}
