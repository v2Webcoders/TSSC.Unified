using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class ScreeningScheduleVM
    {
        [Required(ErrorMessage = "Please select Batch.")]
        public int? BatchId { get; set; }

        [Required(ErrorMessage = "Please select Trainer.")]
        public int? TrainerRegistrationId { get; set; }

        [Required(ErrorMessage = "Screening Date is required.")]
        public DateTime? ScreeningDate { get; set; }

        [Required(ErrorMessage = "Start Time is required.")]
        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [Required(ErrorMessage = "Please select Screening Mode.")]
        public string? ScreeningMode { get; set; }

        public string? Location { get; set; }

        public string? MeetingLink { get; set; }

        public string? Remarks { get; set; }

        public List<SelectListItem> BatchList { get; set; } = new();

        public List<SelectListItem> TrainerList { get; set; } = new();
    }
}
