using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class JobRoleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter QP Code.")]
        public string QPCode { get; set; }

        [Required(ErrorMessage = "Please enter Job Role Title.")]
        public string JobRoleTitle { get; set; }

        public string Description { get; set; }

        public int? SubSectorId { get; set; }

        public string QPVersion { get; set; }

        public string NSQFLevel { get; set; }

        public string EducationQualification { get; set; }

        public decimal? QPHours { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ValidFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ValidTo { get; set; }

        public List<JobRoleDocumentViewModel> Documents { get; set; }
            = new List<JobRoleDocumentViewModel>();
    }

    public class JobRoleDocumentViewModel
    {
        public string? DocumentType { get; set; }

        public string? Language { get; set; }

        public IFormFile? File { get; set; }
    }
}