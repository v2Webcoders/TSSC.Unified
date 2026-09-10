using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class SubSector
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        public string SubSectorName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public virtual ICollection<JobRole> JobRoles { get; set; }
            = new List<JobRole>();
    }
}