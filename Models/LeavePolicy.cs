using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class LeavePolicy
    {
        [Key]
        public int LeavePolicyId { get; set; }

        [Required]
        public int LeaveTypeId { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal NoOfDays { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Property
        [ForeignKey(nameof(LeaveTypeId))]
        public virtual LeaveType? LeaveType { get; set; }
    }
}
