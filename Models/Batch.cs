using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    [Table("Batches", Schema = "dbo")]
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

        [Required]
        public int TPId { get; set; }

        [Required, StringLength(30)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        public ICollection<BatchCandidate> Candidates { get; set; }
            = new List<BatchCandidate>();
    }

    [Table("Candidates")]
    public class Candidate
    {
        public int Id { get; set; }

        public string? ApplicationUserId { get; set; }

        [Required, StringLength(200)]
        public string CandidateName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string MobileNumber { get; set; } = string.Empty;

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public ICollection<BatchCandidate> Enrollments { get; set; }
            = new List<BatchCandidate>();
    }

    [Table("BatchCandidates")]
    public class BatchCandidate
    {
        public int Id { get; set; }

        public int BatchRecordId { get; set; }

        [ForeignKey(nameof(BatchRecordId))]
        public Batch? Batch { get; set; }

        public int CandidateId { get; set; }

        public Candidate? Candidate { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        [StringLength(30)]
        public string Status { get; set; } = "Enrolled";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
    public class CandidateExcelRow
    {
        public string CandidateName { get; set; } = string.Empty;

        public string MobileNumber { get; set; } = string.Empty;

        public string? Email { get; set; }
    }
}
