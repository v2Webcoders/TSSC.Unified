using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class LeaveType
    {
        [Key]
        public int LeaveTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string LeaveTypeName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public decimal? MaxDaysPerYear { get; set; }

        public bool IsPaid { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 1;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
