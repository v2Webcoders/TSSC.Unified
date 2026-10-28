
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
   // [Table("TrainingCenter", Schema = "dbo")]
    public class TrainingCenter
    {
        public int Id { get; set; }

        public int? SchemeId { get; set; }
        public int? ProjectId { get; set; }
        public int JobRoleId { get; set; }

        [Required, StringLength(200)]
        public string TrainingCenterName { get; set; } = string.Empty;

        public string? TrainingCenterCode { get; set; }
        public string? TrainingCenterSPOCName { get; set; }
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

        public decimal? UsableAreaSqFt { get; set; }
        public int? NumberOfClassrooms { get; set; }
        public int? NumberOfFloors { get; set; }
        public int? NumberOfLabs { get; set; }

        public string? CenterDescription { get; set; }

        public string? ApproachRoadPhotoPath { get; set; }
        public string? OutsideClassroomPhotoPath { get; set; }
        public string? InsideClassroomPhotoPath { get; set; }
        public string? LabPhotoPath { get; set; }
        public string? CCTVPhotoPath { get; set; }
        public string? CompanySealPath { get; set; }

        public string? EquipmentDetails { get; set; }

        public string? OrganizationName { get; set; }
        public string? SigningAuthorityName { get; set; }
        public string? MobileNumber { get; set; }
        public string? PANNumber { get; set; }
        public string? EmailId { get; set; }

        public string Status { get; set; } = "Pending";
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? PaymentBy { get; set; }

        public DateTime? PaymentDate { get; set; }
        public string PaymentStatus { get; set; } = "Pending";

        public string? FinanceRemark { get; set; }
        public int? FinanceApprovedBy{ get; set; }
        public DateTime? FinanceApprovedDate { get; set; }
        public string? FinanceStatus { get; set; } = "Pending";
        public DateTime? InspectionDate { get; set; }
        public string? InspectionStatus { get; set; } = "Pending";
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
    }
}
