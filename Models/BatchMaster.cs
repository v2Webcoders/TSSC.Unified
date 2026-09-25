using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class BatchMaster
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string? BatchCode { get; set; }

        [Required]
        [StringLength(200)]
        public string BatchName { get; set; } = string.Empty;

        // Foreign Key - JobRoles
        [Required]
        public int JobRoleId { get; set; }

        [ForeignKey(nameof(JobRoleId))]
        public  JobRole? JobRole { get; set; }

        public int? TPId { get; set; }

        [ForeignKey(nameof(TPId))]
        public TPRegistration? TPRegistration { get; set; }
        // Batch dates
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        // Trainer
        [StringLength(500)]
        public string? BatchMasterTrainer { get; set; }

        // Status
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";

        // Audit fields
        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }
        public int? AssessmentAgencyId { get; set; }

        [ForeignKey(nameof(AssessmentAgencyId))]
        public virtual AssessmentAgency? AssessmentAgency { get; set; }

        public string? AssignedBy { get; set; }

        public DateTime? AssignedDate { get; set; }
        [StringLength(50)]
        public string AgencyApprovalStatus { get; set; } = "Pending";

        public string? VerticalHeadApprovedBy { get; set; }

        public DateTime? VerticalHeadApprovedDate { get; set; }
        public bool EmailSent { get; set; } = false;
    }
}
