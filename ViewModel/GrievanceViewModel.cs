using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class GrievanceViewModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter mobile number.")]
        [RegularExpression(
            @"^[0-9]{10}$",
            ErrorMessage = "Please enter a valid 10 digit mobile number."
        )]
        public string MobileNumber { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please describe your grievance.")]
        [StringLength(
            5000,
            MinimumLength = 10,
            ErrorMessage = "Grievance description must be between 10 and 5000 characters."
        )]
        public string Description { get; set; } = string.Empty;


        public IFormFile? Attachment { get; set; }
    }
}
