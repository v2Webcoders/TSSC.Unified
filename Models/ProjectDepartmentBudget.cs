using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class ProjectDepartmentBudget
    {
        [Key]
        public int BudgetId { get; set; }

        public int ProjectId { get; set; }

        [Required]
        [StringLength(150)]
        public string Department { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BudgetAmount { get; set; }

        [StringLength(500)]
        public string? AllocationPurpose { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
