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
}
