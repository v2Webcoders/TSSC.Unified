using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class ODRequest
    {
        [Key]
        public int ODRequestId { get; set; }


        // =====================================================
        // EMPLOYEE
        // =====================================================

        [Required]
        public int EmployeeId { get; set; }


        // =====================================================
        // OD DATE
        // =====================================================

        [Required(ErrorMessage = "Please select OD date.")]
        public DateTime? ODDate { get; set; }



        // =====================================================
        // OD TYPE
        // =====================================================

        [Required]
        [StringLength(50)]
        public string ODType { get; set; } = "Full Day";


        // =====================================================
        // TIME
        // =====================================================

        public DateTime? RequestedFromTime { get; set; }

        public DateTime? RequestedToTime { get; set; }


        // =====================================================
        // LOCATION
        // =====================================================

        [StringLength(200)]
        public string? Location { get; set; }


        // =====================================================
        // PURPOSE
        // =====================================================

        [Required]
        [StringLength(500)]
        public string Purpose { get; set; } = string.Empty;


        // =====================================================
        // REASON / REMARKS
        // =====================================================

        [StringLength(500)]
        public string? Reason { get; set; }


        // =====================================================
        // ATTACHMENT
        // =====================================================

        [StringLength(500)]
        public string? Attachment { get; set; }


        // =====================================================
        // STATUS
        // =====================================================

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";


        // =====================================================
        // APPROVER
        // =====================================================

        public int? ApproverId { get; set; }


        // =====================================================
        // APPROVAL
        // =====================================================

        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        public string? ApprovalRemarks { get; set; }


        // =====================================================
        // CREATED
        // =====================================================

        public DateTime CreatedDate { get; set; } = DateTime.Now;


        // =====================================================
        // NAVIGATION
        // =====================================================

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }


        [ForeignKey(nameof(ApproverId))]
        public virtual Employee? Approver { get; set; }
    }
}
