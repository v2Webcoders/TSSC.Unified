using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    public class ProjectPlanningViewModel
    {
            public int ProjectId { get; set; }

            public List<ProjectMilestoneInput> Milestones { get; set; }
                = new List<ProjectMilestoneInput>();

            public List<ProjectEmployeeInput> Employees { get; set; }
                = new List<ProjectEmployeeInput>();

            public List<ProjectBudgetInput> Budgets { get; set; }
                = new List<ProjectBudgetInput>();
        }

        public class ProjectMilestoneInput
        {
            [Required]
            public string MilestoneName { get; set; } = string.Empty;

            [Required]
            public DateTime? StartDate { get; set; }

            [Required]
            public DateTime? EndDate { get; set; }

            public int? VendorId { get; set; }
        }

        public class ProjectEmployeeInput
        {
            [Required]
            public int? EmployeeId { get; set; }
        }

        public class ProjectBudgetInput
        {
            [Required]
            public string Department { get; set; } = string.Empty;

            [Required]
            [Range(0.01, double.MaxValue)]
            public decimal BudgetAmount { get; set; }

            public string? AllocationPurpose { get; set; }

            public string? Remarks { get; set; }
        }
    }

