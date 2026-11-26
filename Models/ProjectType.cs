using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class ProjectType
    {
        [Key]
        public int ProjectTypeId { get; set; }

        [Required]
        [StringLength(150)]
        public string ProjectTypeName { get; set; }

        public string? Description { get; set; }

        public int? ParentProjectTypeId { get; set; }

        [ForeignKey("ParentProjectTypeId")]
        public virtual ProjectType? ParentProjectType { get; set; }

        public virtual ICollection<ProjectType> SubProjectTypes { get; set; }
            = new List<ProjectType>();

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string? ModifiedBy { get; set; }
    }
}
