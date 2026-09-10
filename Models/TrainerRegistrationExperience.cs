using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class TrainerRegistrationExperience
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrainerRegistrationId { get; set; }

        [ForeignKey(nameof(TrainerRegistrationId))]
        public TrainerRegistration TrainerRegistration { get; set; }
            = null!;

        [MaxLength(300)]
        public string? OrganizationName { get; set; }

        [MaxLength(200)]
        public string? Designation { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        [MaxLength(100)]
        public string? Duration { get; set; }

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(1000)]
        public string? FilePath { get; set; }

        public DateTime CreatedDate { get; set; }

    }
}
