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

        // =====================================================
        // CHECK-IN LOCATION
        // =====================================================

        public double? CheckInLatitude { get; set; }

        public double? CheckInLongitude { get; set; }

        public double? CheckInAccuracy { get; set; }


        // =====================================================
        // CHECK-OUT LOCATION
        // =====================================================

        public double? CheckOutLatitude { get; set; }

        public double? CheckOutLongitude { get; set; }

        public double? CheckOutAccuracy { get; set; }
        public string? CheckInLocation { get; set; }

        public string? CheckOutLocation { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }
    }
}
