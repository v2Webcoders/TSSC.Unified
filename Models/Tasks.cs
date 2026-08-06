using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class Tasks
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        [StringLength(200)]
        public string TaskName { get; set; }

        public string? TaskDescription { get; set; }

        [Required]
        public int AssignedTo { get; set; }

        [Required]
        public int AssignedBy { get; set; }

        [StringLength(500)]
        public string? Attachment { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime Deadline { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        [StringLength(30)]
        public string? Status { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;
        public string? EmployeeRemarks { get; set; }

        public DateTime? CompletedOn { get; set; }

        [StringLength(500)]
        public string? AttachmentByEmployee { get; set; }
        public bool IsClosed { get; set; }
    }
}
