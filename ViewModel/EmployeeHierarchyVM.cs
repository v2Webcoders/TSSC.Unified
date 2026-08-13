using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class EmployeeHierarchyVM
    {
        public int HierarchyId { get; set; }

        [Required(ErrorMessage = "Please select hierarchy PDF")]
        public IFormFile? HierarchyFile { get; set; }

        public string? HierarchyDocument { get; set; }


        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; }
    }
}
