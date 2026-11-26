using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class ProjectTask
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int MilestoneId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(200)]
        public string TaskName { get; set; } = string.Empty;

        public string? TaskDescription { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Assigned";

        [Required]
        public int AssignedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        public string? EmployeeRemarks { get; set; }

        public string? EmployeeAttachmentPath { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public virtual AdminProject? Project { get; set; }

        [ForeignKey(nameof(MilestoneId))]
        public virtual ProjectMilestone? Milestone { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [ForeignKey(nameof(AssignedBy))]
        public virtual Employee? AssignedByEmployee { get; set; }
    }
}
