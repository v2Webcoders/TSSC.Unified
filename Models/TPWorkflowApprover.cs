using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    [Table("TPWorkflowApprovers")]
    public class TPWorkflowApprover
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string StageCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string StageName { get; set; } = string.Empty;

        public int SequenceNo { get; set; }

        [StringLength(20)]
        public string? ApproverType { get; set; }

        [StringLength(100)]
        public string? RoleName { get; set; }

        [StringLength(450)]
        public string? ApplicationUserId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }
}
