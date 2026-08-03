namespace QUIZAPP.Areas.Admin.ViewModels
{
    public class DealerMasterVM
    {
        public int Id { get; set; }
        public string DealerCode { get; set; }
        public string DealerName { get; set; }
        public string? Pincode { get; set; }
        public string? DealerStatus { get; set; }
        public string DType { get; set; }
        public string DealerTypeText
        {
            get
            {
                return DType == "2w" ? "2-Wheeler"
                     : DType == "4w" ? "4-Wheeler"
                     : "Unknown";
            }
        }
    }

}
