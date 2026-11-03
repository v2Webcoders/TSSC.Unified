using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class EditBatchVM : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Batch ID")]
        public string BatchId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Scheme")]
        public int SchemeId { get; set; }

        [Required]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required]
        [Display(Name = "Training Center")]
        public int TrainingCenterId { get; set; }

        [Required]
        [Display(Name = "Job Role")]
        public int JobRoleId { get; set; }

        [Required]
        [Display(Name = "Trainer")]
        public int TrainerRegistrationId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        // Dropdown lists
        public List<SelectListItem> Schemes { get; set; } = new();
        public List<SelectListItem> Projects { get; set; } = new();
        public List<SelectListItem> TrainingCenters { get; set; } = new();
        public List<SelectListItem> JobRoles { get; set; } = new();
        public List<SelectListItem> Trainers { get; set; } = new();

        // Optional Excel upload
        [Display(Name = "Upload Candidates (Excel)")]
        public IFormFile? CandidateFile { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (EndDate.Date < StartDate.Date)
            {
                yield return new ValidationResult(
                    "End Date cannot be earlier than Start Date.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}


