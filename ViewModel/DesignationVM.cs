using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class DesignationVM
    {
        public int DesignationId { get; set; }
        [Required(ErrorMessage = "Designation Name is required.")]
        public string DesignationName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
