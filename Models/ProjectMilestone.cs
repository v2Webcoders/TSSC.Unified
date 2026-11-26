using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class ProjectMilestone
    {
        [Key]
        public int MilestoneId { get; set; }

        public int ProjectId { get; set; }

        [Required]
        [StringLength(200)]
        public string MilestoneName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int? VendorId { get; set; }

        public DateTime CreatedDate { get; set; }

        [ForeignKey("VendorId")]
        public virtual ProjectVendor? Vendor { get; set; }
    }
}
