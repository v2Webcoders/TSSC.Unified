using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class Holiday
    {
        [Key]
        public int HolidayId { get; set; }

        // =====================================================
        // HOLIDAY DATE
        // =====================================================

        [Required]
        public DateTime HolidayDate { get; set; }

        // =====================================================
        // HOLIDAY NAME
        // =====================================================

        [Required]
        [StringLength(200)]
        public string HolidayName { get; set; } = string.Empty;

        // =====================================================
        // TYPE
        // =====================================================

        [Required]
        [StringLength(50)]
        public string HolidayType { get; set; } = "Public Holiday";

        // =====================================================
        // DESCRIPTION
        // =====================================================

        [StringLength(500)]
        public string? Description { get; set; }

        // =====================================================
        // STATUS
        // =====================================================

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Published";

        // =====================================================
        // CREATED
        // =====================================================

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }
    }
}
