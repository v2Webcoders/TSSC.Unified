using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class TPSignUpVM
    {
        [Required(ErrorMessage = "Organization Name is required.")]
        [StringLength(200)]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Applicant Name is required.")]
        [StringLength(150)]
        [Display(Name = "Applicant Name")]
        public string ApplicantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email ID is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email ID")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        [RegularExpression(@"^[0-9]{10}$",
            ErrorMessage = "Mobile Number must be exactly 10 digits.")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;
    }
}
