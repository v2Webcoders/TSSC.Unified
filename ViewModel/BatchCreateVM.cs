using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class BatchCreateVM
    {
        [Required(ErrorMessage = "Batch Name is required.")]
        public string BatchName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select Job Role.")]
        public int? JobRoleId { get; set; }

        public int? TPId { get; set; }
       
        [Required(ErrorMessage = "Start Date is required.")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End Date is required.")]
        public DateTime? EndDate { get; set; }

        public string? BatchMasterTrainer { get; set; }
        public int? AssessmentAgencyId { get; set; }

        public string? AssignedBy { get; set; }

        public DateTime? AssignedDate { get; set; }
        public List<SelectListItem> JobRoleList { get; set; } = new();

        public List<SelectListItem> TPList { get; set; } = new();
        public List<int> SelectedTrainerIds { get; set; } = new();
    }
}
