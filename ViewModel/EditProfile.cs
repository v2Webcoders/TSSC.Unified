using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class EditProfileVM
    {
        public int EmployeeId { get; set; }

        // Personal Information
        [Required]
        public string FirstName { get; set; }

        public string? LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public DateTime? DOB { get; set; }

        public string BloodGroup { get; set; }

        [Required]
        public string MaritalStatus { get; set; }

        // Photo
        public IFormFile? Photo { get; set; }
        public string? PhotoPath { get; set; }

        // Contact
        public string? PersonalEmail { get; set; }

        [Required]
        public string MobileNo { get; set; }

        public string? AlternateMobile { get; set; }

        // Address
        public string CurrentAddress { get; set; }

        public string? PermanentAddress { get; set; }

        // Identity
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar Number must be exactly 12 digits.")]
        [Display(Name = "Aadhaar Number")]
        public string? AadhaarNo { get; set; }

        [RegularExpression(@"^[A-Za-z0-9]{10}$", ErrorMessage = "PAN Number must be exactly 10 alphanumeric characters.")]
        [Display(Name = "PAN Number")]
        public string? PANNo { get; set; }

        public string? PassportNo { get; set; }

        public string? UANNo { get; set; }

        public string? PFNo { get; set; }

        public string? ESICNo { get; set; }

        // Bank
        public string? BankName { get; set; }

        public string? AccountNo { get; set; }

        public string? IFSCCode { get; set; }
    }
}
