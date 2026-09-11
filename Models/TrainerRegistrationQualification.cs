using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class TrainerRegistrationQualification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrainerRegistrationId { get; set; }

        [ForeignKey(nameof(TrainerRegistrationId))]
        public TrainerRegistration TrainerRegistration { get; set; }
            = null!;

        [MaxLength(200)]
        public string? Qualification { get; set; }

        [MaxLength(300)]
        public string? Board { get; set; }

        [MaxLength(20)]
        public string? PassingYear { get; set; }

        [MaxLength(50)]
        public string? PercentageOrCGPA { get; set; }

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(1000)]
        public string? FilePath { get; set; }

        public DateTime CreatedDate { get; set; }

    }
}
