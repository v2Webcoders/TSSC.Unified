using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{

    [Table("TPRegistrations")]
    public class TPRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string OrganizationType { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string RegistrationNumber { get; set; } = string.Empty;

        public DateTime? DateOfIncorporation { get; set; }

        [Required, StringLength(10)]
        public string PANNumber { get; set; } = string.Empty;

        public bool IsGSTAvailable { get; set; }

        [StringLength(15)]
        public string? GSTNumber { get; set; }

        [Required, StringLength(50)]
        public string SectorExperience { get; set; } = string.Empty;

        public int? YearsOfExperience { get; set; }
        public string? PreviousProjects { get; set; }
        public int? TrainingCentersCount { get; set; }
        public int? TrainersAvailableCount { get; set; }

        [Required]
        public string RegisteredOfficeAddress { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string District { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(6)]
        public string PinCode { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string MobileNumber { get; set; } = string.Empty;

        [StringLength(150)]
        public string? AlternateContactPerson { get; set; }

        [Required]
        public string AnnualTurnoverLast3Years { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string IFSCCode { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string BankName { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string AuthorizedSignatoryName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Designation { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string AuthorizedPersonEmail { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string AuthorizedPersonMobile { get; set; } = string.Empty;

        public bool DeclarationAccepted { get; set; }

        [Required, StringLength(30)]
        public string Status { get; set; } = "Submitted";

        // =====================================================
        // TSSC INTERNAL FIELDS
        // =====================================================

        [StringLength(150)]
        public string? SpocName { get; set; }

        public string? ReviewerRemarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        [StringLength(450)]
        public string? ApplicationUserId { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - REGIONAL HEAD
        // =====================================================

        [StringLength(50)]
        public string? Regional_Head_Status { get; set; } = "Pending";

        public string? Regional_Head_Remarks { get; set; }

        [StringLength(255)]
        public string? Regional_Head_ApprovedBy { get; set; }

        public DateTime? Regional_Head_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - AFFILIATIONS
        // =====================================================

        [StringLength(50)]
        public string? Affiliations_Status { get; set; } = "Pending";

        public string? Affiliations_Remarks { get; set; }

        [StringLength(450)]
        public string? Affiliations_ApprovedBy { get; set; }

        public DateTime? Affiliations_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - FINANCE
        // =====================================================

        [StringLength(50)]
        public string? Finance_Status { get; set; } = "Pending";

        public string? Finance_Remarks { get; set; }

        [StringLength(255)]
        public string? Finance_ApprovedBy { get; set; }

        public DateTime? Finance_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - VERTICAL HEAD
        // =====================================================

        [StringLength(50)]
        public string? Vertical_Head_Status { get; set; } = "Pending";

        public string? Vertical_Head_Remarks { get; set; }

        [StringLength(255)]
        public string? Vertical_Head_ApprovedBy { get; set; }

        public DateTime? Vertical_Head_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - SENIOR REVIEWER 1
        // =====================================================

        [StringLength(50)]
        public string? SeniorReviewer1_Status { get; set; } = "Pending";

        public string? SeniorReviewer1_Remarks { get; set; }

        [StringLength(450)]
        public string? SeniorReviewer1_ApprovedBy { get; set; }

        public DateTime? SeniorReviewer1_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - SENIOR REVIEWER 2
        // =====================================================

        [StringLength(50)]
        public string? SeniorReviewer2_Status { get; set; } = "Pending";

        public string? SeniorReviewer2_Remarks { get; set; }

        [StringLength(450)]
        public string? SeniorReviewer2_ApprovedBy { get; set; }

        public DateTime? SeniorReviewer2_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - CEO
        // =====================================================

        [StringLength(50)]
        public string? CEO_Status { get; set; } = "Pending";

        public string? CEO_Remarks { get; set; }

        [StringLength(255)]
        public string? CEO_ApprovedBy { get; set; }

        public DateTime? CEO_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - PAYMENT PROCESS
        // =====================================================

        [StringLength(50)]
        public string? PaymentProcess_Status { get; set; } = "Pending";

        public string? PaymentProcess_Remarks { get; set; }

        [StringLength(450)]
        public string? PaymentProcess_ApprovedBy { get; set; }

        public DateTime? PaymentProcess_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - FINANCE AFTER PAYMENT
        // =====================================================

        [StringLength(50)]
        public string? FinanceCertificate_Status { get; set; } = "Pending";

        public string? FinanceCertificate_Remarks { get; set; }

        [StringLength(450)]
        public string? FinanceCertificate_ApprovedBy { get; set; }

        public DateTime? FinanceCertificate_Date { get; set; }


        // =====================================================
        // APPROVAL WORKFLOW - CERTIFICATE ISSUANCE
        // =====================================================

        [StringLength(50)]
        public string? CertificateIssuance_Status { get; set; } = "Pending";

        public string? CertificateIssuance_Remarks { get; set; }

        [StringLength(450)]
        public string? CertificateIssuance_ApprovedBy { get; set; }

        public DateTime? CertificateIssuance_Date { get; set; }


        // =====================================================
        // CURRENT WORKFLOW STAGE
        // =====================================================

        [StringLength(50)]
        public string? CurrentStage { get; set; } = "REGIONAL";


        // =====================================================
        // OVERALL APPROVAL STATUS
        // =====================================================

        [StringLength(50)]
        public string? OverallApprovalStatus { get; set; } = "In_Progress";

        public DateTime? OverallApprovalStatusUpdatedOn { get; set; }

        public string? PaymentTransactionId { get; set; }

        public string? PaymentProofFileName { get; set; }

        public string? PaymentProofStatus { get; set; }

        public DateTime? PaymentProofSubmittedOn { get; set; }

        public string? MembershipType { get; set; }

        // =====================================================
        // DOCUMENTS
        // =====================================================

        public ICollection<TPRegistrationDocument> Documents { get; set; }
            = new List<TPRegistrationDocument>();
    }

    [Table("TPRegistrationDocuments")]
    public class TPRegistrationDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TPRegistrationId { get; set; }

        [ForeignKey(nameof(TPRegistrationId))]
        public TPRegistration TPRegistration { get; set; } = null!;

        [Required, StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
    }
}
