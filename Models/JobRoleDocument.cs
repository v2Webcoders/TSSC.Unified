using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class JobRoleDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobRoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; }

        [Required]
        [StringLength(50)]
        public string Language { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Property
        [ForeignKey("JobRoleId")]
        public virtual JobRole JobRole { get; set; }
    }
}
