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
                //EnableSsl = bool.Parse(smtpConfig["EnableSsl"])
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpConfig["UserName"]),
                Subject = subject,
                Body = template,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            mailMessage.Bcc.Add("subodh@v2web.in");
            await smtpClient.SendMailAsync(mailMessage);
            //try
            //{
            //    await smtpClient.SendMailAsync(mailMessage);
            //}
            //catch (Exception ex)
            //{
            //    return ex.Message;
            //}
            return "True";
        }


        // -----------------------------------------------------------------
        // NEW: called by LeaveController after a NEW leave request is saved.
        // Never throws: the leave is already saved, so a mail problem must not fail the request.
        // -----------------------------------------------------------------
        public async Task NotifyLeaveAppliedAsync(int leaveRequestId, string reviewUrl, string templatePath)   // ADJUST id type (int/long)
        {
            //try
            {
                var lr = await _dbContext.LeaveRequest.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.LeaveRequestId == leaveRequestId);
                if (lr == null) return;

                var applicant = await _dbContext.Employee.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == lr.EmployeeId);                         // ADJUST key name
                if (applicant == null) return;

                var manager = await _dbContext.Employee.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == applicant.ReportingManagerId);          // ADJUST key name

                var leaveType = await _dbContext.LeaveType.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.LeaveTypeId == lr.LeaveTypeId);                       // ADJUST key name

                // user-entered text goes into HTML, so it must be encoded
                string H(string s) => WebUtility.HtmlEncode(s ?? string.Empty);

                static string FullName(string first, string last) =>
                $"{first} {last}".Trim();

                string employeeName = FullName(applicant.FirstName, applicant.LastName);
                string managerName = manager != null
                    ? FullName(manager.FirstName, manager.LastName)
                    : "Reporting Manager";
                var common = new Dictionary<string, string>
                {
                    ["EmployeeName"] = H(employeeName),
                    ["LeaveType"] = H(leaveType?.LeaveTypeName),
                    ["LeaveDuration"] = H(Convert.ToString(lr.LeaveDuration)),
                    ["FromDate"] = lr.FromDate.ToString("dd MMM yyyy"),
                    ["ToDate"] = lr.ToDate.ToString("dd MMM yyyy"),
                    ["TotalDays"] = lr.TotalDays.ToString(),
                    ["Reason"] = H(lr.Reason),
                    ["EmergencyContact"] = H(lr.EmergencyContact),
                    ["AppliedOn"] = lr.CreatedDate.ToString("dd MMM yyyy hh:mm tt"),
                    ["ReviewUrl"] = reviewUrl
                };

                string subject = $"Leave request from {employeeName}: {lr.FromDate:dd MMM} - {lr.ToDate:dd MMM yyyy}";

                // ---- Reporting Manager ----
                if (!string.IsNullOrWhiteSpace(manager?.OfficialEmail))                                       // ADJUST
                {
                    var data = new Dictionary<string, string>(common)
                    {
                        ["RecipientName"] = H(managerName),
                        ["Intro"] = $"<strong>{H(employeeName)}</strong> has applied for leave and the request is waiting for your approval."
                    };
                    await TrySendAsync(manager.OfficialEmail, subject, templatePath, data);
                }
                else
                {
                   // _logger.LogWarning("Leave {Id}: reporting manager has no email address.", leaveRequestId);
                }

                // ---- HR: each address gets its own mail ----
                var hrEmails = _configuration.GetSection("LeaveNotifications:HrEmails").Get<string[]>() ?? Array.Empty<string>();

                foreach (var hr in hrEmails.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var data = new Dictionary<string, string>(common)
                    {
                        ["RecipientName"] = "HR Team",
                        ["Intro"] = $"<strong>{H(employeeName)}</strong> has applied for leave. The request has been sent to {H(managerName)} for approval. This is for your information."
                    };
                    await TrySendAsync(hr, subject, templatePath, data);
                }
            }
            //catch (Exception ex)
            //{
            //    //_logger.LogError(ex, "Leave {Id}: notification failed.", leaveRequestId);
            //}
        }

        private async Task TrySendAsync(string to, string subject, string templatePath, Dictionary<string, string> data)
        {
            //try
            {
                await SendEmailAsync(to, subject, templatePath, data);
            }
            //catch (Exception ex)
            //{
            //    //_logger.LogError(ex, "Leave notification to {To} failed.", to);   // one failed recipient does not stop the others
            //}
        }

    }


}

