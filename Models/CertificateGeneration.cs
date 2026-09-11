namespace TSSC.Unified.Models
{
    public class CertificateGeneration
    {
        public int Id { get; set; }

        public int CertificateId { get; set; }

        public DateTime GeneratedDate { get; set; }

        public string? GeneratedBy { get; set; }

        public string? ExcelFileName { get; set; }

        public int TotalRecords { get; set; }

        public int SuccessRecords { get; set; }

        public int FailedRecords { get; set; }

        public bool IsActive { get; set; }

        // Navigation Property
        public Certificate? Certificate { get; set; }
        public int? StateId { get; set; }

    }
}
