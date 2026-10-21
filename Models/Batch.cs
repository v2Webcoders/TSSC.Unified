using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class Batch
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string BatchId { get; set; } = string.Empty;

        [Required]
        public int SchemeId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int TrainingCenterId { get; set; }

        [Required]
        public int JobRoleId { get; set; }

        [Required]
        public int TrainerRegistrationId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required, StringLength(450)]
        public string TPUserId { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        public ICollection<BatchCandidate> Candidates { get; set; }
            = new List<BatchCandidate>();
    }
    public class BatchCandidate
    {
        public int Id { get; set; }

        public int BatchRecordId { get; set; }

        public Batch? Batch { get; set; }

        [Required, StringLength(200)]
        public string CandidateName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string MobileNumber { get; set; } = string.Empty;

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
