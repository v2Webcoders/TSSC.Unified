namespace TSSC.Unified.ViewModel
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Http;
    public class PayNowVM
    {
        public int Id { get; set; }

        public string? OrganizationName { get; set; }

        public string? PaymentProofStatus { get; set; }

        [Required(ErrorMessage = "Please enter the transaction ID / UTR number.")]
        [StringLength(100, ErrorMessage = "Transaction ID cannot exceed 100 characters.")]
        [Display(Name = "Transaction ID / UTR Number")]
        public string PaymentTransactionId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please upload your payment screenshot.")]
        [Display(Name = "Payment Screenshot")]
        public IFormFile? PaymentScreenshot { get; set; }

        public string QRCodePath { get; set; } =
            "/assets/images/tssc_qrcode.png";

    }
}
