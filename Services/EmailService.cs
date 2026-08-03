using QUIZAPP.Areas.Admin.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;

namespace QUIZAPP.Services
{
    public class EmailService
    {
        private AppdbContext _dbContext;
        private readonly IConfiguration _configuration;
        public EmailService(AppdbContext context, IConfiguration configuration)
        {
            _dbContext = context;
            _configuration = configuration;
        }
        public  async Task<string> SendEmailAsync(string toEmail, string subject, string templatePath, Dictionary<string, string> replacements)
        {
            string filePath = templatePath;

            //Populate Html Template
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (replacements == null)
                throw new ArgumentNullException(nameof(replacements));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified template file was not found.", filePath);

            string template = File.ReadAllText(filePath);

            foreach (var replacement in replacements)
            {
                if (replacement.Key == null)
                    throw new ArgumentException("Replacement key cannot be null.", nameof(replacements));

                template = template.Replace($"{{{{{replacement.Key}}}}}", replacement.Value ?? string.Empty);
            }
            //End PopulateHtmlTemplate


            var smtpConfig = _configuration.GetSection("Smtp");
            var smtpClient = new SmtpClient(smtpConfig["Host"])
            {
                Port = int.Parse(smtpConfig["Port"]),
                Credentials = new NetworkCredential(smtpConfig["UserName"], smtpConfig["Password"]),
                EnableSsl = bool.Parse(smtpConfig["EnableSsl"])
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpConfig["UserName"]),
                Subject = subject,
                Body = template,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            mailMessage.CC.Add("ksubodhs@yahoo.co.in");
            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "True";
        }
    }
}

