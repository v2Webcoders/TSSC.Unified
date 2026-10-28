
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TSSC.Unified.ViewModel
{
    public class TrainingCenterViewModel
    {
        public int Id { get; set; }


        [Required(ErrorMessage = "Please select a Job Role.")]
        public int JobRoleId { get; set; }

        public string? JobRoleTitle { get; set; }

        [Required(ErrorMessage = "Please enter Training Center Name.")]
        [StringLength(200)]
        public string TrainingCenterName { get; set; } = string.Empty;

        public string? TrainingCenterCode { get; set; }
        public string? TrainingCenterSPOCName { get; set; }

        [EmailAddress]
        public string? TrainingCenterSPOCEmail { get; set; }

        public string? SPOCContactNumber { get; set; }
        public string? TrainingCenterPhoneNumber { get; set; }
        public string? CompleteAddress { get; set; }
        public string? CenterFloor { get; set; }
        public string? Landmark { get; set; }

        public string? CenterOwnershipType { get; set; }
        public string? FranchisePartnerName { get; set; }
        public string? CenterType { get; set; }

        public string? SelfFunded { get; set; }
        public string? NSDC { get; set; }
        public string? STAR { get; set; }
        public string? MoRD { get; set; }
        public string? StateGovernmentSchemes { get; set; }
        public string? OtherCentralGovernmentSchemes { get; set; }

        [Range(0, 999999999)]
        public decimal? UsableAreaSqFt { get; set; }

        [Range(0, 10000)]
        public int? NumberOfClassrooms { get; set; }

        [Range(0, 1000)]
        public int? NumberOfFloors { get; set; }

        [Range(0, 10000)]
        public int? NumberOfLabs { get; set; }

        public string? CenterDescription { get; set; }

        public IFormFile? ApproachRoadPhoto { get; set; }
        public IFormFile? OutsideClassroomPhoto { get; set; }
        public IFormFile? InsideClassroomPhoto { get; set; }
        public IFormFile? LabPhoto { get; set; }
        public IFormFile? CCTVPhoto { get; set; }
        public IFormFile? CompanySeal { get; set; }

        public string? EquipmentDetails { get; set; }

        public string? OrganizationName { get; set; }
        public string? SigningAuthorityName { get; set; }
        public string? MobileNumber { get; set; }
        public string? PANNumber { get; set; }

        [EmailAddress]
        public string? EmailId { get; set; }

        public string? Status { get; set; }
        public string? PaymentBy { get; set; }

        public DateTime? PaymentDate { get; set; }
        public string? PaymentStatus { get; set; }
        public string? FinanceRemark { get; set; }
        public int? FinanceApprovedBy { get; set; }
        public DateTime? FinanceApprovedDate { get; set; }
        public string? FinanceStatus { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string? InspectionStatus { get; set; }
        public DateTime? InspectionCreatedDate { get; set; }
        public string? InspectionLink { get; set; }
        public string? InspectionMode { get; set; }
        public string? InspectionRemarks { get; set; }
        public int? RegionalHeadEmployeeId { get; set; }

        public string? RegionalHeadStatus { get; set; }

        public string? RegionalHeadRemarks { get; set; }

        public DateTime? SentToRegionalHeadDate { get; set; }
        public DateTime? RegionalHeadApproveDate { get; set; }
        public int? VerticalHeadEmployeeId { get; set; }

        public string? VerticalHeadStatus { get; set; }

        public string? VerticalHeadRemarks { get; set; }

        public DateTime? VerticalHeadDateApproveDate { get; set; }
        public int? BusinessHeadEmployeeId { get; set; }
        public string? BusinessHeadStatus { get; set; }
        public string? BusinessHeadRemarks { get; set; }
        public DateTime? BusinessHeadApproveDate { get; set; }
        public List<SelectListItem> JobRoles { get; set; } = new();
        public decimal? QPHours { get; set; }
        public int? SchemeId { get; set; }
        public string? SchemeName { get; set; }
        public List<SelectListItem> Scheme { get; set; }
            = new List<SelectListItem>();
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public List<SelectListItem> Project { get; set; }
            = new List<SelectListItem>();
    }
}
