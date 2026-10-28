using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProjectName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
