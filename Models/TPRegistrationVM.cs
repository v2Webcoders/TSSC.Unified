using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class TPRegistrationVM
    {
        [Required(ErrorMessage = "Organization Name is required.")]
        [StringLength(200)]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type of Organization is required.")]
        [Display(Name = "Type of Organization")]
        public string OrganizationType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Registration Number is required.")]
        [StringLength(100)]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Official Email ID is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Official Email ID")]
        public string OfficialEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        [RegularExpression(@"^[0-9]{10}$",
            ErrorMessage = "Mobile Number must be exactly 10 digits.")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Authorized Person Name is required.")]
        [StringLength(150)]
        [Display(Name = "Authorized Person Name")]
        public string AuthorizedPersonName { get; set; } = string.Empty;
    }
}