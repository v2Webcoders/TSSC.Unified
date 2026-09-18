using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TSSC.Unified.Models;

namespace TSSC.Unified.Models
{
    public class AssessmentAgency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string AgencyName { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }
    }
}
