using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class AdminProject
    {
        [Key]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Please select project type")]
        public int ProjectTypeId { get; set; }
        public int? ProjectSubTypeId { get; set; }

        [Required(ErrorMessage = "Project name is required")]
        [StringLength(200)]
        public string ProjectName { get; set; }

        [StringLength(50)]
        public string Department { get; set; }

        public string ProjectDescription { get; set; }

        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ProjectCost { get; set; }

        public int? ProjectLocationId { get; set; }

        public int ProjectManagerId { get; set; }

        public int BusinessHeadId { get; set; }

        [StringLength(500)]
        public string Attachment { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;
        public string? BusinessHeadStatus { get; set; }

        public string? BusinessHeadRemarks { get; set; }

        public int? BusinessHeadReviewedBy { get; set; }

        public DateTime? BusinessHeadReviewedDate { get; set; }
        // CEO Planning Approval Details

        public string? CEOStatus { get; set; }

        public string? CEORemarks { get; set; }

        public int? CEOReviewedBy { get; set; }

        public DateTime? CEOReviewedDate { get; set; }

        public DateTime? SubmittedToCEODate { get; set; }

        [StringLength(30)]
        public string ProjectApprovalStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string InfrastructureSetupStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string TrainerAssignmentStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string BatchCreationStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string TrainingStartStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string AssessmentStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string CertificationStatus { get; set; } = "Pending";
    }
}
