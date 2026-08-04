namespace TSSC.Unified.Models
{
    public class Branch
    {
        public int BranchId { get; set; }

        public string BranchName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }
    }
    public class SubBranch
    {
        public int SubBranchId { get; set; }

        public int? BranchId { get; set; }

        public string SubBranchName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }
    }
}
