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
        public virtual JobRole? JobRole { get; set; }

        [ForeignKey(nameof(TPId))]
        public int? TPId { get; set; }

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
    }
}
