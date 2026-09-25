using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class TPRegistrationVM
    {
        // Basic Details
        [Required, StringLength(200)]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Type of Organization")]
        public string OrganizationType { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Incorporation")]
        public DateTime? DateOfIncorporation { get; set; }

        [Required]
        [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$",
            ErrorMessage = "Enter a valid PAN number.")]
        public string PANNumber { get; set; } = string.Empty;

        [RegularExpression(@"^$|^[0-9A-Z]{15}$",
            ErrorMessage = "GST Number must be 15 characters.")]
        public string? GSTNumber { get; set; }

        // Organization Profile
        [Required]
        public string SectorExperience { get; set; } = string.Empty;

        [Required, Range(0, 100)]
        public int? YearsOfExperience { get; set; }

        public string? PreviousProjects { get; set; }

        [Range(0, int.MaxValue)]
        public int? TrainingCentersCount { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int? TrainersAvailableCount { get; set; }

        // Contact Details
        [Required]
        public string RegisteredOfficeAddress { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string District { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Pin Code must be 6 digits.")]
        public string PinCode { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Official Email ID")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile Number must be exactly 10 digits.")]
        public string MobileNumber { get; set; } = string.Empty;

        public string? AlternateContactPerson { get; set; }

        // Financial Details
        [Required]
        public string AnnualTurnoverLast3Years { get; set; } = string.Empty;

        [Required]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        public string IFSCCode { get; set; } = string.Empty;

        [Required]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public IFormFile? FinancialDocuments { get; set; }

        // Documents
        [Required]
        public IFormFile? RegistrationCertificate { get; set; }

        [Required]
        public IFormFile? PANCardCopy { get; set; }

        public IFormFile? GSTCertificate { get; set; }

        [Required]
        public IFormFile? AddressProof { get; set; }

        [Required]
        public IFormFile? CancelledCheque { get; set; }

        [Required]
        public IFormFile? UndertakingDocument { get; set; }

        // Authorized Person
        [Required]
        public string AuthorizedSignatoryName { get; set; } = string.Empty;

        [Required]
        public string Designation { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string AuthorizedPersonEmail { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[0-9]{10}$")]
        public string AuthorizedPersonMobile { get; set; } = string.Empty;

        [Required]
        public IFormFile? AuthorizedPersonIdProof { get; set; }

        // Declaration
        [Range(typeof(bool), "true", "true",
            ErrorMessage = "You must accept the declaration before submitting.")]
        public bool DeclarationAccepted { get; set; }
    }

    public class TPRegistrationEditVM
    {
        public int Id { get; set; }

        // =====================================================
        // BASIC DETAILS
        // =====================================================

        [Required, StringLength(200)]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Type of Organization")]
        public string OrganizationType { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Incorporation")]
        public DateTime? DateOfIncorporation { get; set; }

        [Required]
        [RegularExpression(
            @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$",
            ErrorMessage = "Enter a valid PAN number.")]
        public string PANNumber { get; set; } = string.Empty;

        [RegularExpression(
            @"^$|^[0-9A-Z]{15}$",
            ErrorMessage = "GST Number must be 15 characters.")]
        public string? GSTNumber { get; set; }


        // =====================================================
        // ORGANIZATION PROFILE
        // =====================================================

        [Required]
        public string SectorExperience { get; set; } = string.Empty;

        [Required, Range(0, 100)]
        public int? YearsOfExperience { get; set; }

        public string? PreviousProjects { get; set; }

        [Range(0, int.MaxValue)]
        public int? TrainingCentersCount { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int? TrainersAvailableCount { get; set; }


        // =====================================================
        // CONTACT DETAILS
        // =====================================================

        [Required]
        public string RegisteredOfficeAddress { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string District { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^[0-9]{6}$",
            ErrorMessage = "Pin Code must be 6 digits.")]
        public string PinCode { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Official Email ID")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^[0-9]{10}$",
            ErrorMessage = "Mobile Number must be exactly 10 digits.")]
        public string MobileNumber { get; set; } = string.Empty;

        public string? AlternateContactPerson { get; set; }


        // =====================================================
        // FINANCIAL DETAILS
        // =====================================================

        [Required]
        public string AnnualTurnoverLast3Years { get; set; } = string.Empty;

        [Required]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        public string IFSCCode { get; set; } = string.Empty;

        [Required]
        public string BankName { get; set; } = string.Empty;


        // =====================================================
        // DOCUMENTS
        // IMPORTANT: NO [Required] HERE
        // Existing documents can be retained.
        // =====================================================

        public IFormFile? FinancialDocuments { get; set; }

        public IFormFile? RegistrationCertificate { get; set; }

        public IFormFile? PANCardCopy { get; set; }

        public IFormFile? GSTCertificate { get; set; }

        public IFormFile? AddressProof { get; set; }

        public IFormFile? CancelledCheque { get; set; }

        public IFormFile? UndertakingDocument { get; set; }


        // =====================================================
        // AUTHORIZED PERSON
        // =====================================================

        [Required]
        public string AuthorizedSignatoryName { get; set; } = string.Empty;

        [Required]
        public string Designation { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string AuthorizedPersonEmail { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^[0-9]{10}$",
            ErrorMessage = "Mobile Number must be exactly 10 digits.")]
        public string AuthorizedPersonMobile { get; set; } = string.Empty;

        // Existing ID proof can be retained
        public IFormFile? AuthorizedPersonIdProof { get; set; }


        // =====================================================
        // DECLARATION
        // =====================================================

        [Range(
            typeof(bool),
            "true",
            "true",
            ErrorMessage = "You must accept the declaration before submitting.")]
        public bool DeclarationAccepted { get; set; }
    }
}
