using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class JobRole
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string QPCode { get; set; }

        [Required]
        [StringLength(250)]
        public string JobRoleTitle { get; set; }

        public string Description { get; set; }

        public int? SubSectorId { get; set; }
        [ForeignKey("SubSectorId")]
        public virtual SubSector SubSector { get; set; }

        [StringLength(50)]
        public string QPVersion { get; set; }

        [StringLength(50)]
        public string NSQFLevel { get; set; }

        [StringLength(500)]
        public string EducationQualification { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? QPHours { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public string? Remark { get; set; } 

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Property
        public virtual ICollection<JobRoleDocument> Documents { get; set; }
            = new List<JobRoleDocument>();
       

       
    }
}
