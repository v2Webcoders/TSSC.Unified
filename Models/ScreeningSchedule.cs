using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class ScreeningSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BatchId { get; set; }

        [Required]
        public int TrainerRegistrationId { get; set; }

        [Required]
        public DateTime ScreeningDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [StringLength(50)]
        public string? ScreeningMode { get; set; }

        [StringLength(500)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? MeetingLink { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Scheduled";

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public bool EmailSent { get; set; } = false;

        // Navigation Properties

        [ForeignKey(nameof(BatchId))]
        public virtual BatchMaster? Batch { get; set; }

        [ForeignKey(nameof(TrainerRegistrationId))]
        public virtual TrainerRegistration? TrainerRegistration { get; set; }
    }
}