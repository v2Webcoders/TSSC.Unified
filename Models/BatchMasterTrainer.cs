using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class BatchMasterTrainer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BatchId { get; set; }

        [Required]
        public int TrainerRegistrationId { get; set; }

        public DateTime CreatedDate { get; set; }

        // Navigation Properties

        [ForeignKey(nameof(BatchId))]
        public virtual BatchMaster? Batch { get; set; }

        [ForeignKey(nameof(TrainerRegistrationId))]
        public virtual TrainerRegistration? TrainerRegistration { get; set; }
    }
}