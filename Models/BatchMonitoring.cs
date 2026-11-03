using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class BatchMonitoring
    {
        public int Id { get; set; }

        [Required]
        public int BatchId { get; set; }

        [Required]
        public DateTime MonitoringDateTime { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Scheduled";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        // Navigation property
        [ForeignKey(nameof(BatchId))]
        public Batch Batch { get; set; } = null!;
    }
}
