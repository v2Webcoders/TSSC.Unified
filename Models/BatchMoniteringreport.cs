using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class BatchMonitoringReport
    {
        public int Id { get; set; }

        [Required]
        public int BatchMonitoringId { get; set; }

        [ForeignKey(nameof(BatchMonitoringId))]
        public BatchMonitoring BatchMonitoring { get; set; } = null!;

        [Required]
        public string SubmittedByUserId { get; set; } = string.Empty;

        public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;

        // Infrastructure Verification
        public bool LabAvailable { get; set; }
        public bool InternetAvailable { get; set; }
        public bool EquipmentAvailable { get; set; }
        public bool ClassroomAvailable { get; set; }

        // Attendance Verification
        public bool CandidateAttendanceVerified { get; set; }
        public bool AttendanceUploadedInSystem { get; set; }
        public bool BiometricVerified { get; set; }

        // Trainer Verification
        public bool TrainerPresent { get; set; }
        public bool SessionConductedProperly { get; set; }
        public bool TrainingMaterialAvailable { get; set; }

        // Training Quality Verification
        public bool PracticalSessionConducted { get; set; }
        public bool LMSUsageVerified { get; set; }
        public bool CandidateFeedbackCollected { get; set; }

        [StringLength(2000)]
        public string? Remarks { get; set; }

        // Uploaded file paths
        [StringLength(500)]
        public string? MonitoringPhotosPath { get; set; }

        [StringLength(500)]
        public string? SupportingDocumentPath { get; set; }
    }
}
