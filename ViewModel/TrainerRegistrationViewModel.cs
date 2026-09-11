using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class TrainerRegistrationViewModel
    {
        // =========================
        // BASIC DETAILS
        // =========================

        [Required(ErrorMessage = "Candidate Name is required.")]
        public string CandidateName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required.")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please select Gender.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select Job Role.")]
        public int? JobRoleId { get; set; }

        public List<SelectListItem> JobRoleList { get; set; }
            = new List<SelectListItem>();

        //[Required(ErrorMessage = "Please select TP / AB.")]
        public int? TPId { get; set; }

        public string? SPOCName { get; set; }

        public string? SIDHApplied { get; set; }

        public string? Scheme { get; set; }


        // =========================
        // CONTACT DETAILS
        // =========================

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select State.")]
        public int? StateId { get; set; }
        public List<SelectListItem> StateList { get; set; }
         = new List<SelectListItem>();
        [Required(ErrorMessage = "Please select District.")]
        public int? DistrictId { get; set; }
        public List<SelectListItem> DistrictList { get; set; }
        = new List<SelectListItem>();
        [Required(ErrorMessage = "Please select City.")]
        public int? CityId { get; set; }
        public List<SelectListItem> CityList { get; set; }
        = new List<SelectListItem>();
        [Required(ErrorMessage = "Pincode is required.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "Please enter a valid 6 digit pincode.")]
        public string Pincode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        [RegularExpression(
            @"^[6-9]\d{9}$",
            ErrorMessage = "Please enter a valid 10 digit mobile number.")]
        public string Mobile { get; set; } = string.Empty;

        public bool MobileVerified { get; set; }


        // =========================
        // REFERENCE DETAILS
        // =========================

        public string? ReferenceName { get; set; }

        public string? ReferenceDesignation { get; set; }

        public string? ReferenceOrganization { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid reference email.")]
        public string? ReferenceEmail { get; set; }

        public string? ReferenceMobile { get; set; }


        // =========================
        // QUALIFICATION
        // =========================

        public List<TrainerQualificationVM> Qualifications { get; set; }
            = new List<TrainerQualificationVM>();


        // =========================
        // EXPERIENCE
        // =========================

        public List<TrainerExperienceVM> Experiences { get; set; }
            = new List<TrainerExperienceVM>();


        // =========================
        // DOCUMENTS
        // =========================

        public IFormFile? AadhaarFile { get; set; }

        public IFormFile? PanCardFile { get; set; }


        // =========================
        // DECLARATION
        // =========================

        [Range(
            typeof(bool),
            "true",
            "true",
            ErrorMessage = "Please accept Terms & Conditions.")]
        public bool TermsAccepted { get; set; }

        [Range(
            typeof(bool),
            "true",
            "true",
            ErrorMessage = "Please accept the declaration.")]
        public bool AuthenticityAccepted { get; set; }
    }


    public class TrainerQualificationVM
    {
        public string? Qualification { get; set; }

        public string? Board { get; set; }

        public string? PassingYear { get; set; }

        public string? PercentageOrCGPA { get; set; }

        public IFormFile? File { get; set; }
    }


    public class TrainerExperienceVM
    {
        public string? OrganizationName { get; set; }

        public string? Designation { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? Duration { get; set; }

        public IFormFile? File { get; set; }
    }
}
