public class Certificate
{
    public int Id { get; set; }

    public string CertificateNo { get; set; }

    public string CompanyName { get; set; }

    public string Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsActive { get; set; }
    public string? TemplateFile { get; set; }
    public string? CertificateType { get; set; }
}