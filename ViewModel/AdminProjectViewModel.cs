using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class AdminProjectViewModel
    {
        public int ProjectId { get; set; }
        public int? ProjectSubTypeId { get; set; }

        public List<SelectListItem> ProjectSubTypeList { get; set; }
            = new List<SelectListItem>();

        [Required(ErrorMessage = "Please select project type")]
        public int ProjectTypeId { get; set; }

        [Required(ErrorMessage = "Project name is required")]
        [StringLength(200)]
        public string ProjectName { get; set; }

        // Department is a normal text field
        [StringLength(50)]
        public string Department { get; set; }

        [Required(ErrorMessage = "Project description is required")]
        public string ProjectDescription { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

       

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today;
        public decimal? ProjectCost { get; set; }

        public int? ProjectLocationId { get; set; }

        [Required(ErrorMessage = "Please select project manager")]
        public int? ProjectManagerId { get; set; }

        // Business Head from UserIn.Role
        [Required(ErrorMessage = "Please select business head")]
        public int BusinessHeadId { get; set; }

        // Single attachment
        public IFormFile AttachmentFile { get; set; }
        public string? BusinessHeadStatus { get; set; }

        public string? BusinessHeadRemarks { get; set; }

        public int? BusinessHeadReviewedBy { get; set; }

        public DateTime? BusinessHeadReviewedDate { get; set; }

        // Dropdowns
        public List<SelectListItem> ProjectTypeList { get; set; }
            = new List<SelectListItem>();

        public List<SelectListItem> ProjectLocationList { get; set; }
            = new List<SelectListItem>();

        public List<SelectListItem> ProjectManagerList { get; set; }
            = new List<SelectListItem>();

        public List<SelectListItem> BusinessHeadList { get; set; }
            = new List<SelectListItem>();
    }
}
