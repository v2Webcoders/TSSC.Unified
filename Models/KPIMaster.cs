using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class KpiMaster
    {
        public int Id { get; set; }
        public string KpiName { get; set; } = "";
        public string Unit { get; set; } = "₹ Cr";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalValue { get; set; }
        public decimal ReceivedValue { get; set; }

        [NotMapped]
        public decimal PendingValue => TotalValue - ReceivedValue;

        public string CreatedBy { get; set; } = "";
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
