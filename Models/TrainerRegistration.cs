using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.Models
{
        public class TrainerRegistration
        {
            [Key]
            public int Id { get; set; }

            [MaxLength(50)]
            public string? RegistrationNo { get; set; }

            [MaxLength(20)]
            public string? RegistrationMode { get; set; }

            // Basic Details
            [Required]
            [MaxLength(200)]
            public string CandidateName { get; set; } = string.Empty;

            public DateTime? DateOfBirth { get; set; }

            [MaxLength(20)]
            public string? Gender { get; set; }

            public int? JobRoleId { get; set; }

            public int? TPId { get; set; }

            [MaxLength(200)]
            public string? SPOCName { get; set; }

            [MaxLength(10)]
            public string? SIDHApplied { get; set; }

            [MaxLength(200)]
            public string? Scheme { get; set; }


            // Contact Details
            [MaxLength(1000)]
            public string? Address { get; set; }

            public int? StateId { get; set; }

            public int? DistrictId { get; set; }

            public int? CityId { get; set; }

            [MaxLength(10)]
            public string? Pincode { get; set; }

            [MaxLength(200)]
            public string Email { get; set; }
        public bool EmailVerified { get; set; }

        [MaxLength(20)]
            public string? Mobile { get; set; }

            public bool MobileVerified { get; set; }


            // Reference Details
            [MaxLength(200)]
            public string? ReferenceName { get; set; }

            [MaxLength(200)]
            public string? ReferenceDesignation { get; set; }

            [MaxLength(300)]
            public string? ReferenceOrganization { get; set; }

            [MaxLength(200)]
            public string? ReferenceEmail { get; set; }

            [MaxLength(20)]
            public string? ReferenceMobile { get; set; }


            // Declaration
            public bool TermsAccepted { get; set; }

            public bool AuthenticityAccepted { get; set; }

            public DateTime? DeclarationDate { get; set; }


            // Workflow
            [MaxLength(50)]
            public string? Status { get; set; }

            [MaxLength(1000)]
            public string? RejectionReason { get; set; }

            [MaxLength(1000)]
            public string? Remarks { get; set; }


            // Audit
            public DateTime CreatedDate { get; set; }

            public DateTime? UpdatedDate { get; set; }

            public int? CreatedBy { get; set; }

            public int? UpdatedBy { get; set; }


            // Child Records
            public ICollection<TrainerRegistrationQualification>
                Qualifications
            { get; set; }
                = new List<TrainerRegistrationQualification>();

            public ICollection<TrainerRegistrationExperience>
                Experiences
            { get; set; }
                = new List<TrainerRegistrationExperience>();

            public ICollection<TrainerRegistrationDocument>
                Documents
            { get; set; }
                = new List<TrainerRegistrationDocument>();
        
    }
}
