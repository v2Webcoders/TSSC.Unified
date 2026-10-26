using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class BatchMonitoringVM
    {
        public int BatchId { get; set; }

        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a monitoring date and time.")]
        [Display(Name = "Date & Time")]
        public DateTime? MonitoringDateTime { get; set; }
    }
    public class BatchMonitoringReportVM : IValidatableObject
    {
        public int BatchMonitoringId { get; set; }

        public int BatchId { get; set; }

        public string? BatchNumber { get; set; }

        public DateTime? MonitoringDateTime { get; set; }

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

        [StringLength(2000, ErrorMessage = "Remarks cannot exceed 2000 characters.")]
        public string? Remarks { get; set; }

        public IFormFile? MonitoringPhotos { get; set; }

        public IFormFile? SupportingDocument { get; set; }

        public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Remarks))
            {
                yield return new ValidationResult(
                    "Remarks are required.",
                    new[] { nameof(Remarks) });
            }

            if (MonitoringPhotos == null || MonitoringPhotos.Length == 0)
            {
                yield return new ValidationResult(
                    "Please upload monitoring photos.",
                    new[] { nameof(MonitoringPhotos) });
            }

            if (SupportingDocument == null || SupportingDocument.Length == 0)
            {
                yield return new ValidationResult(
                    "Please upload the supporting PDF document.",
                    new[] { nameof(SupportingDocument) });
            }
        }
    }
}
