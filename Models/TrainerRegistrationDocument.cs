using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class TrainerRegistrationDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrainerRegistrationId { get; set; }

        [ForeignKey(nameof(TrainerRegistrationId))]
        public TrainerRegistration TrainerRegistration { get; set; }
            = null!;

        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(1000)]
        public string? FilePath { get; set; }

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long? FileSize { get; set; }

        [MaxLength(50)]
        public string? VerificationStatus { get; set; }

        [MaxLength(1000)]
        public string? VerificationRemarks { get; set; }

        public int? VerifiedBy { get; set; }

        public DateTime? VerifiedDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? CreatedBy { get; set; }

    }
}
