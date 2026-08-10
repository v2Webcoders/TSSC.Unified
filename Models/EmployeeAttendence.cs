using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class EmployeeAttendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        public DateTime? InTime { get; set; }

        public DateTime? OutTime { get; set; }

        [Required]
        [StringLength(20)]
        public string AttendanceStatus { get; set; } = "Present";

        [StringLength(500)]
        public string? Remarks { get; set; }

        [Required]
        [StringLength(50)]
        public string AttendanceSource { get; set; } = "Biometric";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Property
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; }
    }
}
