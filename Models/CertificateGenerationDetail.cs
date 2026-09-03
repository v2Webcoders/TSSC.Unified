namespace TSSC.Unified.Models
{
    public class CertificateGenerationDetail
    {
        public int Id { get; set; }

        public int CertificateGenerationId { get; set; }

        public string? CertificateNo { get; set; }

        public string? CompanyName { get; set; }

        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? GeneratedFileName { get; set; }

        public string? GeneratedFilePath { get; set; }

        public DateTime GeneratedDate { get; set; }

        public bool IsGenerated { get; set; }

        public string? ErrorMessage { get; set; }

        public bool IsActive { get; set; }

        // Navigation Property
        public CertificateGeneration? CertificateGeneration { get; set; }
    }
}
