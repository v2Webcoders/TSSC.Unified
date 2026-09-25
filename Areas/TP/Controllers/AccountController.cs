using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.TP.Controllers
{
    [Area("TP")]
    public class AccountController : Controller
    {
        private readonly AppdbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AccountController(
            AppdbContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }


        // =========================================================
        // GET: /TPRegistration/Account/SignUp
        // =========================================================

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }


        // =========================================================
        // POST: /TPRegistration/Account/SignUp
        // =========================================================


        // =========================================================
        // SUCCESS PAGE
        // =========================================================

        [HttpGet]
        public IActionResult SignUpSuccess(string username)
        {
            ViewBag.Username = username;

            return View();
        }


        // =========================================================
        // GENERATE USERNAME
        // =========================================================

        private async Task<string> GenerateUsernameAsync()
        {
            string username;

            do
            {
                var random = Random.Shared.Next(100000, 999999);

                username = $"TP{random}";

            } while (
                await _userManager.FindByNameAsync(username) != null
            );

            return username;
        }


        // =========================================================
        // GENERATE TEMP PASSWORD
        // =========================================================

        private string GenerateTemporaryPassword()
        {
            var random = Random.Shared.Next(100000, 999999);

            return $"Tp@{random}";
        }


        // =========================================================
        // SEND EMAIL
        // =========================================================

        private async Task SendEmailAsync(
            string toEmail,
            string subject,
            string templateName,
            Dictionary<string, string> replacements)
        {
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "EmailTemplates",
                templateName);


            if (!System.IO.File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    "Email template not found.",
                    templatePath);
            }


            var body = await System.IO.File.ReadAllTextAsync(
                templatePath);


            foreach (var replacement in replacements)
            {
                body = body.Replace(
                    $"{{{{{replacement.Key}}}}}",
                    replacement.Value ?? "");
            }


            var smtpSection =
                _configuration.GetSection("Smtp");


            var host = smtpSection["Host"];

            var port = int.Parse(
                smtpSection["Port"] ?? "587");

            var username = smtpSection["Username"];

            var password = smtpSection["Password"];

            var enableSsl = bool.Parse(
                smtpSection["EnableSsl"] ?? "true");


            using var message =
                new System.Net.Mail.MailMessage();


            message.From =
                new System.Net.Mail.MailAddress(
                    username!,
                    "TSSC");

            message.To.Add(toEmail);

            message.Subject = subject;

            message.Body = body;

            message.IsBodyHtml = true;


            using var smtp =
                new System.Net.Mail.SmtpClient(
                    host,
                    port);


            smtp.Credentials =
                new System.Net.NetworkCredential(
                    username,
                    password);

            smtp.EnableSsl = enableSsl;


            await smtp.SendMailAsync(message);
        }
    }
}