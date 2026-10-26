namespace TSSC.Unified.Models
{
    public class Grievance
    {
        
            public int Id { get; set; }

            public string GrievanceNo { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public string MobileNumber { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public string Status { get; set; } = "Submitted";

            public string Priority { get; set; } = "Normal";

            public string? AttachmentPath { get; set; }

            public DateTime CreatedDate { get; set; }

            public DateTime? UpdatedDate { get; set; }

            public DateTime? ResolvedDate { get; set; }
            public string? TeamRemarks { get; set; }

            public string? ReportPath { get; set; }

            public int? ResolvedBy { get; set; }
            public bool IsActive { get; set; } = true;
          }
    }
