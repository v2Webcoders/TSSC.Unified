using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class HRPolicy
    {
        [Key]
        public int PolicyId { get; set; }

        [Required]
        public string PolicyTitle { get; set; }

        public string? Description { get; set; }

        public string? PolicyDocument { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
    }

    public class HRPolicyVM
    {
        public int PolicyId { get; set; }

        [Required(ErrorMessage = "Policy Title is required.")]
        [Display(Name = "Policy Title")]
        [StringLength(200)]
        public string PolicyTitle { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        // Existing PDF file name
        public string? PolicyDocument { get; set; }

        [Display(Name = "Upload Policy PDF")]
        public IFormFile? PolicyFile { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created On")]
        public DateTime? CreatedOn { get; set; }

        [Display(Name = "Modified By")]
        public string? ModifiedBy { get; set; }

        [Display(Name = "Modified On")]
        public DateTime? ModifiedOn { get; set; }

        // Helper property for UI
        public bool IsEdit => PolicyId > 0;
    }
}
