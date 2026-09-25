using Microsoft.AspNetCore.Mvc;

namespace TSSC.Unified.Controllers
{
    public class HomeController : Controller
    {
        //public IActionResult Index()
        //{
        //    return RedirectToAction("Index", "Home", new { area = "Admin" });
        //}
        [HttpGet("certificate-verify")]
        public IActionResult CertificateVerify(string? certificateId)
        {
            return Content(
                $"Certificate verification is coming soon.\n\nCertificate ID: {certificateId}"
            );
        }
    }
}
