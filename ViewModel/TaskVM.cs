using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class TaskVM
    {
        public int TaskId { get; set; }

        [Required]
        [Display(Name = "Task Name")]
        public string TaskName { get; set; }

        [Display(Name = "Task Description")]
        public string? TaskDescription { get; set; }

        [Required]
        [Display(Name = "Assign To")]
        public int AssignedTo { get; set; }

        public int AssignedBy { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile? AttachmentFile { get; set; }

        public string? Attachment { get; set; }

        [Required]
        [Display(Name = "Deadline")]
        [DataType(DataType.Date)]
        public DateTime Deadline { get; set; } = DateTime.Today;

        public string? Remarks { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;
        //for employees
        public string? AssignedToName { get; set; }
        public string? AssignedByName { get; set; }
        public string? EmployeeRemarks { get; set; }

        public DateTime? CompletedOn { get; set; }

        [Display(Name = "AttachmentByEmployee")]
        public IFormFile? AttachmentByEmployeeFile { get; set; }

        public string? AttachmentByEmployee { get; set; }
        public bool IsClosed { get; set; }

    }
}