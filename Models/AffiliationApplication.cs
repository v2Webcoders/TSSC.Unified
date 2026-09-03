using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
    public class AffiliationApplication
    {
        public int AffiliationApplicationId { get; set; }

        [StringLength(30)]
        public string? ApplicationNo { get; set; }

        // =====================================================
        // APPLICANT
        // =====================================================

        [Required]
        public string ApplicantUserId { get; set; } = string.Empty;


        // =====================================================
        // BASIC DETAILS
        // =====================================================

        [Required]
        [StringLength(200)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string OrganizationType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        public DateTime? DateOfIncorporation { get; set; }

        [Required]
        [StringLength(10)]
        public string PANNumber { get; set; } = string.Empty;

        [StringLength(15)]
        public string? GSTNumber { get; set; }


        // =====================================================
        // ORGANIZATION PROFILE
        // =====================================================

        [Required]
        public string SectorExperience { get; set; } = string.Empty;

        [Required]
        [Range(0, 100)]
        public int? YearsOfExperience { get; set; }

        public string? PreviousProjects { get; set; }

        [Range(0, int.MaxValue)]
        public int? NumberOfTrainingCenters { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int? NumberOfTrainers { get; set; }


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
        [StringLength(6)]
        public string PinCode { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string OfficialEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string MobileNumber { get; set; } = string.Empty;

        public string? AlternateContactPerson { get; set; }


        // =====================================================
        // FINANCIAL DETAILS
        // =====================================================

        [Required]
        public string AnnualTurnover { get; set; } = string.Empty;

        [Required]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        public string IFSCCode { get; set; } = string.Empty;

        [Required]
        public string BankName { get; set; } = string.Empty;


        // =====================================================
        // AUTHORIZED PERSON
        // =====================================================

        [Required]
        public string AuthorizedPersonName { get; set; } = string.Empty;

        [Required]
        public string AuthorizedPersonDesignation { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string AuthorizedPersonEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string AuthorizedPersonMobile { get; set; } = string.Empty;


        // =====================================================
        // DECLARATION
        // =====================================================

        public bool TermsAccepted { get; set; }

        public bool AuthenticityDeclaration { get; set; }


        // =====================================================
        // APPLICATION STATUS
        // =====================================================

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime? SubmittedOn { get; set; }


        // =====================================================
        // AUDIT
        // =====================================================

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }


        // =====================================================
        // DOCUMENTS
        // =====================================================

        public ICollection<AffiliationDocument> Documents { get; set; }
            = new List<AffiliationDocument>();
    }
    public class AffiliationDocument
    {
        public int AffiliationDocumentId { get; set; }

        public int AffiliationApplicationId { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        public AffiliationApplication? AffiliationApplication { get; set; }
    }
}
