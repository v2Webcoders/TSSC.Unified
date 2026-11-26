using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class ProjectMilestoneUpdate
    {
        [Key]
        public int UpdateId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int MilestoneId { get; set; }

        [Required]
        public DateTime UpdateDate { get; set; }

        [Required]
        [StringLength(30)]
        public string CurrentStatus { get; set; } = "Not Started";

        [Required]
        [Range(0, 100)]
        public decimal ProgressPercentage { get; set; }

        [StringLength(500)]
        public string? SupportingDocument { get; set; }

        public string? Remarks { get; set; }

        [Required]
        public int UpdatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ProjectId))]
        public virtual AdminProject? Project { get; set; }

        [ForeignKey(nameof(MilestoneId))]
        public virtual ProjectMilestone? Milestone { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public virtual Employee? Employee { get; set; }
    }
}
