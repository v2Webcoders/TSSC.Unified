using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class Scheme
    {
        [Key]
        public int SchemeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SchemeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string SchemeName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }
    }
}
