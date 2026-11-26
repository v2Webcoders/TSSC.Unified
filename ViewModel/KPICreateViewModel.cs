using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class KpiCreateViewModel
    {
        public List<KpiItemViewModel> Items { get; set; } = new();
    }

    public class KpiItemViewModel
    {
        public string KpiName { get; set; } = "";   // fixed by the controller

        [Required(ErrorMessage = "Total is required")]
        [Range(0, 999999999, ErrorMessage = "Enter 0 or more")]
        public decimal? TotalValue { get; set; }

        // Dates belong to the Total value only
        [Required(ErrorMessage = "From date is required")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Required(ErrorMessage = "To date is required")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        // No dates for Received (Pending is calculated)
        [Required(ErrorMessage = "Received is required")]
        [Range(0, 999999999, ErrorMessage = "Enter 0 or more")]
        public decimal? ReceivedValue { get; set; }
    }
}
