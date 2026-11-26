using QUIZAPP.Areas.Admin.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using Microsoft.CodeAnalysis;

namespace QUIZAPP.Services
{
    public class EmailService
    {
        private AppdbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        private string BaseUrl => (_configuration["AppSettings:AbsPath"] ?? string.Empty).TrimEnd('/');
        private bool IsUat => string.Equals(_configuration["AppSettings:Mode"], "UAT", StringComparison.OrdinalIgnoreCase);
        private string TesterEmail => _configuration["AppSettings:TesterEmail"];
        public EmailService(AppdbContext context, IConfiguration configuration, ILogger<EmailService> logger)   // NEW parameter
        {
            _dbContext = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> SendEmailAsync(string toEmail, string subject, string templatePath, Dictionary<string, string> replacements)
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
            var appSettings = _configuration.GetSection("AppSettings");
            //string mode = appSettings["Mode"];

            var smtpClient = new SmtpClient(smtpConfig["Host"])
            {
                Port = int.Parse(smtpConfig["Port"]),
                Credentials = new NetworkCredential(smtpConfig["UserName"], smtpConfig["Password"]),
                EnableSsl = bool.Parse(smtpConfig["EnableSsl"])
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpConfig["FromEmail"]),
                Subject = subject,
                Body = template,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            // Development / UAT: Tester copy
            if (!string.IsNullOrWhiteSpace(appSettings["TesterEmail"]))
            {
                mailMessage.Bcc.Add(appSettings["TesterEmail"]);
            }


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
                    _logger.LogWarning("Leave {Id}: reporting manager has no email address.", leaveRequestId);
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
            //    _logger.LogError(ex, "Leave {Id}: notification failed.", leaveRequestId);
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
            //    _logger.LogError(ex, "Leave notification to {To} failed.", to);   // one failed recipient does not stop the others
            //}
        }

        // =====================================================================
        // Leave APPROVED (or rejected) email: employee + approver + HR
        // Add NotifyLeaveDecisionAsync to EmailService (next to NotifyLeaveAppliedAsync).
        // It uses the same TrySendAsync (UAT redirect), BaseUrl, _dbContext, _configuration, _logger.
        // Lines marked  // ADJUST  use property names I had to guess.
        // =====================================================================

        // ---------------------------------------------------------------------
        // 1. EmailService
        // ---------------------------------------------------------------------
        public async Task NotifyLeaveDecisionAsync(
            int leaveRequestId,            // ADJUST id type (int/long)
            string decision,               // "Approved" or "Rejected"
            int decidedByEmployeeId,       // ADJUST id type
            DateTime decidedOn,
            string employeePath,           // page the employee opens   e.g. Url.Action("MyLeaves", "Leave")
            string managerPath,            // page for approver / HR    e.g. Url.Action("ManageLeaves", "Leave")
            string templatePath)
        {
            //try
            {
                var lr = await _dbContext.LeaveRequest.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.LeaveRequestId == leaveRequestId);
                if (lr == null) return;

                var applicant = await _dbContext.Employee.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == lr.EmployeeId);                         // ADJUST
                var approver = await _dbContext.Employee.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == decidedByEmployeeId);                   // ADJUST

                var reportingManager = await _dbContext.Employee.AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeId == applicant.ReportingManagerId);

                var leaveType = await _dbContext.LeaveType.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.LeaveTypeId == lr.LeaveTypeId);                       // ADJUST
                if (applicant == null || approver == null) return;

                string H(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
                static string FullName(string first, string last) => $"{first} {last}".Trim();

                bool approved = string.Equals(decision, "Approved", StringComparison.OrdinalIgnoreCase);
                string verb = approved ? "approved" : "rejected";

                string employeeName = FullName(applicant.FirstName, applicant.LastName);
                string approverName = FullName(approver.FirstName, approver.LastName);

                string Url(string path) => BaseUrl + "/" + (path ?? string.Empty).TrimStart('/');

                var common = new Dictionary<string, string>
                {
                    ["EmployeeName"] = H(employeeName),
                    ["LeaveType"] = H(leaveType?.LeaveTypeName),
                    ["LeaveDuration"] = H(Convert.ToString(lr.LeaveDuration)),
                    ["FromDate"] = lr.FromDate.ToString("dd MMM yyyy"),
                    ["ToDate"] = lr.ToDate.ToString("dd MMM yyyy"),
                    ["TotalDays"] = lr.TotalDays.ToString(),
                    ["Reason"] = H(lr.Reason),
                    ["StatusText"] = approved ? "Approved" : "Rejected",
                    ["StatusColor"] = approved ? "#16a34a" : "#dc2626",
                    ["DecisionByLabel"] = approved ? "Approved By" : "Rejected By",
                    ["DecisionOnLabel"] = approved ? "Approved On" : "Rejected On",
                    ["DecidedBy"] = H(approverName),
                    ["DecidedOn"] = decidedOn.ToString("dd MMM yyyy hh:mm tt")
                };

                string subject = $"Leave {verb}: {employeeName} ({lr.FromDate:dd MMM} - {lr.ToDate:dd MMM yyyy})";

                // one mail per distinct address (an approver who is also in the HR list gets a single mail)
                var sent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                async Task SendOnce(string to, string recipientName, string intro, string path)
                {
                    if (string.IsNullOrWhiteSpace(to) || !sent.Add(to.Trim())) return;

                    var data = new Dictionary<string, string>(common)
                    {
                        ["RecipientName"] = H(recipientName),
                        ["Intro"] = intro,
                        ["ReviewUrl"] = Url(path)
                    };
                    await TrySendAsync(to.Trim(), subject, templatePath, data);
                }

                // 1) employee
                await SendOnce(applicant.OfficialEmail, employeeName,                                           // ADJUST Email
                    $"Your leave request has been <strong>{verb}</strong> by <strong>{H(approverName)}</strong>.",
                    employeePath);

                // 2) the person who approved (confirmation copy)
                await SendOnce(approver.OfficialEmail, approverName,                                            // ADJUST Email
                    $"You have <strong>{verb}</strong> the leave request of <strong>{H(employeeName)}</strong>. This is a confirmation copy.",
                    managerPath);
                // 2b) the reporting manager, when someone else (e.g. HR) made the decision
                if (reportingManager != null && reportingManager.EmployeeId != approver.EmployeeId)   // ADJUST key name
                {
                    string managerName = FullName(reportingManager.FirstName, reportingManager.LastName);

                    await SendOnce(reportingManager.OfficialEmail, managerName,                               // ADJUST Email
                        $"The leave request of <strong>{H(employeeName)}</strong> has been <strong>{verb}</strong> by <strong>{H(approverName)}</strong>. This is for your information.",
                        managerPath);
                }

                // 3) HR
                var hrEmails = _configuration.GetSection("LeaveNotifications:HrEmails").Get<string[]>() ?? Array.Empty<string>();
                foreach (var hr in hrEmails)
                {
                    await SendOnce(hr, "HR Team",
                        $"<strong>{H(approverName)}</strong> has <strong>{verb}</strong> the leave request of <strong>{H(employeeName)}</strong>. This is for your information.",
                        managerPath);
                }
            }
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Leave {Id}: decision notification failed.", leaveRequestId);
            //}
        }

        // =====================================================================
        // Attendance regularization approved by HR: email employee + reporting manager + HR
        // Lines marked  // ADJUST  use names I had to guess.
        // =====================================================================

        // ---------------------------------------------------------------------
        // 1. EmailService: new method (uses _dbContext, _configuration, _logger, BaseUrl, TrySendAsync)
        // ---------------------------------------------------------------------
        public async Task NotifyRegularizationDecisionAsync(
            int regularizationId,            // ADJUST id type
            string decision,                 // "Approved" or "Rejected"
            int decidedByEmployeeId,         // ADJUST id type
            DateTime decidedOn,
            string resultingStatus,          // Present / Late / Half Day / Absent (what the attendance became)
            string employeePath,
        string managerPath,
            string templatePath, string hrPath = null)
        {
            try
            {
                var req = await _dbContext.AttendanceRegularization.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.RegularizationId == regularizationId);
                if (req == null) return;

                var applicant = await _dbContext.Employee.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == req.EmployeeId);                        // ADJUST
                var decider = await _dbContext.Employee.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == decidedByEmployeeId);                   // ADJUST
                if (applicant == null || decider == null) return;

                var manager = await _dbContext.Employee.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == applicant.ReportingManagerId);          // ADJUST

                string H(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
                static string FullName(string first, string last) => $"{first} {last}".Trim();
                static string T(DateTime? t) => t.HasValue ? t.Value.ToString("hh:mm tt") : "-";

                bool approved = string.Equals(decision, "Approved", StringComparison.OrdinalIgnoreCase);
                string verb = approved ? "approved" : "rejected";

                string employeeName = FullName(applicant.FirstName, applicant.LastName);
                string deciderName = FullName(decider.FirstName, decider.LastName);
                string managerName = manager == null ? "Reporting Manager" : FullName(manager.FirstName, manager.LastName);

                // the attendance row after approval holds the final in/out times
                var att = await _dbContext.EmployeeAttendance.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == req.EmployeeId && x.AttendanceDate == req.AttendanceDate);

                string Link(string path) => BaseUrl + "/" + (path ?? string.Empty).TrimStart('/');

                var common = new Dictionary<string, string>
                {
                    ["EmployeeName"] = H(employeeName),
                    ["AttendanceDate"] = req.AttendanceDate.ToString("dd MMM yyyy"),
                    ["InTime"] = T(approved ? att?.InTime ?? req.RequestedInTime : req.RequestedInTime),
                    ["OutTime"] = T(approved ? att?.OutTime ?? req.RequestedOutTime : req.RequestedOutTime),
                    ["ResultStatus"] = approved ? H(resultingStatus) : "-",
                    ["Reason"] = H(req.Reason),                                              // ADJUST
                    ["StatusText"] = approved ? "Approved" : "Rejected",
                    ["StatusColor"] = approved ? "#16a34a" : "#dc2626",
                    ["DecisionByLabel"] = approved ? "Approved By" : "Rejected By",
                    ["DecisionOnLabel"] = approved ? "Approved On" : "Rejected On",
                    ["DecidedBy"] = H(deciderName),
                    ["DecidedOn"] = decidedOn.ToString("dd MMM yyyy hh:mm tt")
                };

                string subject = $"Attendance regularization {verb}: {employeeName} ({req.AttendanceDate:dd MMM yyyy})";

                var sent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);       // one mail per address

                async Task SendOnce(string to, string recipientName, string intro, string path)
                {
                    if (string.IsNullOrWhiteSpace(to) || !sent.Add(to.Trim())) return;
                    var data = new Dictionary<string, string>(common)
                    {
                        ["RecipientName"] = H(recipientName),
                        ["Intro"] = intro,
                        ["ReviewUrl"] = Link(path),
                        ["Remarks"] = H(req.ApprovalRemarks),
                    };
                    await TrySendAsync(to.Trim(), subject, templatePath, data);
                }

                // 1) employee
                await SendOnce(applicant.OfficialEmail, employeeName,                                          // ADJUST Email
                    $"Your attendance regularization request for <strong>{req.AttendanceDate:dd MMM yyyy}</strong> has been <strong>{verb}</strong> by <strong>{H(deciderName)}</strong>.",
                    employeePath);

                // 2) reporting manager
                if (manager != null)
                {
                    await SendOnce(manager.OfficialEmail, managerName,                                         // ADJUST Email
                        $"The attendance regularization request of <strong>{H(employeeName)}</strong> for <strong>{req.AttendanceDate:dd MMM yyyy}</strong> has been <strong>{verb}</strong> by <strong>{H(deciderName)}</strong>. This is for your information.",
                        managerPath);
                }

                // 3) HR: the person who decided (confirmation copy) and the HR list
                await SendOnce(decider.OfficialEmail, deciderName,                                             // ADJUST Email
                    $"You have <strong>{verb}</strong> the attendance regularization request of <strong>{H(employeeName)}</strong>. This is a confirmation copy.",
                    managerPath);

                foreach (var hr in _configuration.GetSection("LeaveNotifications:HrEmails").Get<string[]>() ?? Array.Empty<string>())
                {
                    await SendOnce(hr, "HR Team",
                        $"<strong>{H(deciderName)}</strong> has <strong>{verb}</strong> the attendance regularization request of <strong>{H(employeeName)}</strong>. This is for your information.",
                        hrPath ?? managerPath);                                  // CHANGED
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Regularization {Id}: notification failed.", regularizationId);
            }
        }


        public async Task NotifyRegularizationSubmittedAsync(
            int regularizationId,
            string employeePath,
            string managerPath,
            string hrPath,
            string templatePath)
        {
            try
            {
                var req = await _dbContext.AttendanceRegularization
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.RegularizationId == regularizationId);

                if (req == null) return;

                var applicant = await _dbContext.Employee
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == req.EmployeeId);

                if (applicant == null) return;

                var manager = await _dbContext.Employee
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == applicant.ReportingManagerId);

                string H(string s) =>
                    WebUtility.HtmlEncode(s ?? string.Empty);

                string FullName(string first, string last) =>
                    $"{first} {last}".Trim();

                string T(DateTime? t) =>
                    t.HasValue ? t.Value.ToString("hh:mm tt") : "-";

                string employeeName =
                    FullName(applicant.FirstName, applicant.LastName);

                string managerName = manager == null
                    ? "Reporting Manager"
                    : FullName(manager.FirstName, manager.LastName);

                string subject =
                    $"Attendance regularization submitted: {employeeName} ({req.AttendanceDate:dd MMM yyyy})";

                var sent = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

                var common = new Dictionary<string, string>
                {
                    ["EmployeeName"] = H(employeeName),

                    ["AttendanceDate"] =
        req.AttendanceDate.ToString("dd MMM yyyy"),

                    ["InTime"] = T(req.RequestedInTime),

                    ["OutTime"] = T(req.RequestedOutTime),

                    ["RegularizationType"] = H(req.RegularizationType),


                    ["StatusText"] = "Pending",

                    ["StatusColor"] = "#d97706",

                    ["DecisionByLabel"] = "Reporting Manager",

                    ["DecisionOnLabel"] = "Submitted On",

                    ["DecidedBy"] = H(managerName),

                    ["DecidedOn"] =
        req.CreatedDate.ToString("dd MMM yyyy hh:mm tt")
                };

                async Task SendOnce(
                    string to,
                    string recipientName,
                    string intro,
                    string path)
                {
                    if (string.IsNullOrWhiteSpace(to) ||
                        !sent.Add(to.Trim()))
                        return;

                    var data = new Dictionary<string, string>(common)
                    {
                        ["RecipientName"] = H(recipientName),

                        ["Intro"] = intro,

                        ["ReviewUrl"] =
                            BaseUrl + "/" + (path ?? "").TrimStart('/'),

                        ["Reason"] = H(req.Reason),

                        ["Remarks"] = ""
                    };

                    await TrySendAsync(
                        to.Trim(),
                        subject,
                        templatePath,
                        data);
                }

                // =========================================
                // 1. EMPLOYEE - SUBMISSION CONFIRMATION
                // =========================================

                await SendOnce(
                    applicant.OfficialEmail,
                    employeeName,
                    $"Your attendance regularization request for <strong>{req.AttendanceDate:dd MMM yyyy}</strong> has been submitted successfully and is awaiting approval from your reporting manager.",
                    employeePath);

                // =========================================
                // 2. REPORTING MANAGER - ACTION REQUIRED
                // =========================================

                if (manager != null)
                {
                    await SendOnce(
                        manager.OfficialEmail,
                        managerName,
                        $"<strong>{H(employeeName)}</strong> has submitted an attendance regularization request for <strong>{req.AttendanceDate:dd MMM yyyy}</strong>. Please review the request and take appropriate action.",
                        managerPath);
                }

                // =========================================
                // 3. HR TEAM - INFORMATION
                // =========================================

                foreach (var hr in _configuration
                    .GetSection("LeaveNotifications:HrEmails")
                    .Get<string[]>() ?? Array.Empty<string>())
                {
                    await SendOnce(
                        hr,
                        "HR Team",
                        $"<strong>{H(employeeName)}</strong> has submitted an attendance regularization request for <strong>{req.AttendanceDate:dd MMM yyyy}</strong>. The request is pending approval with the reporting manager. This is for your information.",
                        hrPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Regularization {Id}: submission notification failed.",
                    regularizationId);
            }
        }
        public async Task NotifyODSubmittedAsync(
    int odRequestId,
    string templatePath)
        {
            try
            {
                var req = await _dbContext.ODRequest
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ODRequestId == odRequestId);

                if (req == null) return;

                var applicant = await _dbContext.Employee
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == req.EmployeeId);

                if (applicant == null) return;

                var manager = await _dbContext.Employee
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == applicant.ReportingManagerId);

                string H(string s) =>
                    WebUtility.HtmlEncode(s ?? string.Empty);

                string FullName(string first, string last) =>
                    $"{first} {last}".Trim();

                string T(DateTime? t) =>
                    t.HasValue ? t.Value.ToString("hh:mm tt") : "-";

                string employeeName =
                    FullName(applicant.FirstName, applicant.LastName);

                string managerName = manager == null
                    ? "Reporting Manager"
                    : FullName(manager.FirstName, manager.LastName);

                string subject =
                    $"OD Request Submitted: {employeeName} ({req.ODDate:dd MMM yyyy})";

                var sent = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase);

                var common = new Dictionary<string, string>
                {
                    ["EmployeeName"] = H(employeeName),
                    ["ODDate"] = req.ODDate.Value.ToString("dd MMM yyyy"),
                    ["ODType"] = H(req.ODType),
                    ["FromTime"] = T(req.RequestedFromTime),
                    ["ToTime"] = T(req.RequestedToTime),
                    ["Location"] = H(req.Location),
                    ["Purpose"] = H(req.Purpose),
                    ["Reason"] = H(req.Reason),
                    ["ResultStatus"] = "Pending Approval",
                    ["StatusText"] = "Pending",
                    ["StatusColor"] = "#d97706",
                    ["DecisionByLabel"] = "Reporting Manager",
                    ["DecisionOnLabel"] = "Submitted On",
                    ["DecidedBy"] = H(managerName),
                    ["DecidedOn"] = req.CreatedDate.ToString("dd MMM yyyy hh:mm tt")
                };

                async Task SendOnce(string? to)
                {
                    if (string.IsNullOrWhiteSpace(to))
                        return;

                    to = to.Trim();

                    if (!sent.Add(to))
                    {
                        _logger.LogInformation(
                            "Duplicate OD recipient skipped: {Email}", to);
                        return;
                    }

                    var data = new Dictionary<string, string>(common)
                    {
                        ["RecipientName"] = "Team",
                        ["Intro"] =
                            $"An OD request has been submitted by <strong>{H(employeeName)}</strong> for <strong>{req.ODDate.Value:dd MMM yyyy}</strong>. Please find the request details below."
                    };

                    _logger.LogInformation(
                        "Sending OD notification {Id} to {Email}",
                        odRequestId, to);

                    await TrySendAsync(
                        to,
                        subject,
                        templatePath,
                        data);
                }

                // Employee
                await SendOnce(applicant.OfficialEmail);

                // Reporting Manager
                if (manager != null)
                    await SendOnce(manager.OfficialEmail);

                // HR
                var hrEmails = _configuration
                    .GetSection("LeaveNotifications:HrEmails")
                    .Get<string[]>() ?? Array.Empty<string>();

                foreach (var hr in hrEmails)
                    await SendOnce(hr);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "OD Request {Id}: submission notification failed.",
                    odRequestId);
            }
        }

        public async Task NotifyODDecisionAsync(
    int odRequestId,
    int decidedByEmployeeId,
    string decision,
    string templatePath)
        {
            try
            {
                decision = decision?.Trim();

                if (decision != "Approved" && decision != "Rejected")
                    return;

                var req = await _dbContext.ODRequest
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ODRequestId == odRequestId);

                if (req == null) return;

                var applicant = await _dbContext.Employee
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == req.EmployeeId);

                if (applicant == null) return;

                var approver = await _dbContext.Employee
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == decidedByEmployeeId);

                string H(string s) =>
                    WebUtility.HtmlEncode(s ?? string.Empty);

                string FullName(string first, string last) =>
                    $"{first} {last}".Trim();

                string T(DateTime? t) =>
                    t.HasValue ? t.Value.ToString("hh:mm tt") : "-";

                string employeeName =
                    FullName(applicant.FirstName, applicant.LastName);

                string approverName = approver == null
                    ? "Approver"
                    : FullName(approver.FirstName, approver.LastName);

                bool isApproved = decision == "Approved";

                string subject =
                    $"OD Request {decision}: {employeeName} ({req.ODDate.Value:dd MMM yyyy})";

                string decisionMessage = isApproved
                    ? $"The OD request submitted by <strong>{H(employeeName)}</strong> for <strong>{req.ODDate.Value:dd MMM yyyy}</strong> has been approved."
                    : $"The OD request submitted by <strong>{H(employeeName)}</strong> for <strong>{req.ODDate.Value:dd MMM yyyy}</strong> has been rejected.";

                var sent = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

                var common = new Dictionary<string, string>
                {
                    ["EmployeeName"] = H(employeeName),

                    ["ODDate"] =
                        req.ODDate.Value.ToString("dd MMM yyyy"),

                    ["ODType"] = H(req.ODType),

                    ["FromTime"] = T(req.RequestedFromTime),

                    ["ToTime"] = T(req.RequestedToTime),

                    ["Location"] = H(req.Location),

                    ["Purpose"] = H(req.Purpose),

                    ["Reason"] = H(req.Reason),

                    ["ResultStatus"] = decision,

                    ["StatusText"] = decision,

                    ["StatusColor"] = isApproved
                        ? "#16a34a"
                        : "#dc2626",

                    ["DecisionByLabel"] = "Decision By",

                    ["DecisionOnLabel"] = "Decision On",

                    ["DecidedBy"] = H(approverName),

                    ["DecidedOn"] = DateTime.Now.ToString(
                        "dd MMM yyyy hh:mm tt")
                };

                async Task SendOnce(string to)
                {
                    if (string.IsNullOrWhiteSpace(to) ||
                        !sent.Add(to.Trim()))
                        return;

                    var data = new Dictionary<string, string>(common)
                    {
                        ["RecipientName"] = "Team",

                        ["Intro"] = decisionMessage,

                        ["ReviewUrl"] = ""
                    };

                    await TrySendAsync(
                        to.Trim(),
                        subject,
                        templatePath,
                        data);
                }

                // 1. EMPLOYEE
                await SendOnce(applicant.OfficialEmail);

                // 2. APPROVING MANAGER / APPROVER
                if (approver != null)
                    await SendOnce(approver.OfficialEmail);

                // 3. HR TEAM
                foreach (var hr in _configuration
                    .GetSection("LeaveNotifications:HrEmails")
                    .Get<string[]>() ?? Array.Empty<string>())
                {
                    await SendOnce(hr);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "OD Request {Id}: {Decision} notification failed.",
                    odRequestId,
                    decision);
            }
        }

    }
}

