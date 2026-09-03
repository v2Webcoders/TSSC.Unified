using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class DepartmentVM
    {
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Department Name is required.")]
        [Display(Name = "Department Name")]
        public string DepartmentName { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
