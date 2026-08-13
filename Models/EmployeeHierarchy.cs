using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class EmployeeHierarchy
    {
        [Key]
        public int HierarchyId { get; set; }

        public string? HierarchyDocument { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; }
    }
}
