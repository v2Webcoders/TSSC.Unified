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

        public bool ResultUploaded { get; set; } = false;

        public DateTime? ResultUploadedDate { get; set; }

        public bool ResultEmailSent { get; set; } = false;

        public DateTime? ResultEmailSentDate { get; set; }


        // =========================================================
        // NAVIGATION
        // =========================================================

        [ForeignKey(nameof(BatchId))]
        public virtual BatchMaster? Batch { get; set; }

        [ForeignKey(nameof(TrainerRegistrationId))]
        public virtual TrainerRegistration? TrainerRegistration { get; set; }
       

    }
}