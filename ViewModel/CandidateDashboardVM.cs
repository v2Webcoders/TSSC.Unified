namespace TSSC.Unified.ViewModel
{
    public class CandidateDashboardVM
    {
        public int EnrolledBatches { get; set; }
        public int CompletedBatches { get; set; }

        public List<BatchIndexVM> RecentBatches { get; set; } = new();
    }
}
