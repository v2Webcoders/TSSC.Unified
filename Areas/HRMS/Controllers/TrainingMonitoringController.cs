using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "MonitoringTeam")]
    public class TrainingMonitoringController : Controller
    {
        private readonly AppdbContext _context;
        private readonly EmailService _emailService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public TrainingMonitoringController(
            AppdbContext context,
            EmailService emailService,
            UserManager<AppUser> userManager,
            IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _environment = environment;
            _configuration = configuration;
        }

        // GET: /HRMS/TrainingMonitoring/BatchList
        public async Task<IActionResult> BatchList()
        {
            var batches = await _context.Batches
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedOn)
                .Select(b => new BatchIndexVM
                {
                    Id = b.Id,
                    BatchId = b.BatchId,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Status = b.Status,
                    CreatedOn = b.CreatedOn,

                    CandidateCount = _context.BatchCandidates
                        .Count(c => c.BatchRecordId == b.Id),

                    SchemeName = _context.Scheme
                        .Where(s => s.SchemeId == b.SchemeId)
                        .Select(s => s.SchemeName)
                        .FirstOrDefault(),

                    CenterName = _context.TrainingCenter
                        .Where(c => c.Id == b.TrainingCenterId)
                        .Select(c => c.OrganizationName)
                        .FirstOrDefault(),

                    JobRoleName = _context.JobRoles
                        .Where(j => j.Id == b.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault(),

                    ProjectName = _context.Project
                        .Where(p => p.Id == b.ProjectId)
                        .Select(p => p.ProjectName)
                        .FirstOrDefault(),

                    // Latest monitoring record
                    MonitoringId = _context.BatchMonitorings
                        .Where(m => m.BatchId == b.Id)
                        .OrderByDescending(m => m.CreatedOn)
                        .ThenByDescending(m => m.Id)
                        .Select(m => (int?)m.Id)
                        .FirstOrDefault(),

                    MonitoringDateTime = _context.BatchMonitorings
                        .Where(m => m.BatchId == b.Id)
                        .OrderByDescending(m => m.CreatedOn)
                        .ThenByDescending(m => m.Id)
                        .Select(m => (DateTime?)m.MonitoringDateTime)
                        .FirstOrDefault(),

                    MonitoringStatus = _context.BatchMonitorings
                        .Where(m => m.BatchId == b.Id)
                        .OrderByDescending(m => m.CreatedOn)
                        .ThenByDescending(m => m.Id)
                        .Select(m => m.Status)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(batches);
        }

        // GET: /HRMS/TrainingMonitoring/ScheduleScreening?batchId=1
        [HttpGet]
        public async Task<IActionResult> ScheduleScreening(int batchId)
        {
            var batch = await _context.Batches
                .FirstOrDefaultAsync(x => x.Id == batchId);

            if (batch == null)
                return NotFound();

            var existingMonitoring = await _context.BatchMonitorings
                .Where(x => x.BatchId == batchId)
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync();

            var vm = new BatchMonitoringVM
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchId,
                MonitoringDateTime = existingMonitoring?.MonitoringDateTime
            };

            ViewBag.IsEdit = existingMonitoring != null;

            return View(vm);
        }

        // POST: /HRMS/TrainingMonitoring/ScheduleScreening

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleScreening(BatchMonitoringVM vm)
        {
            var batch = await _context.Batches
                .FirstOrDefaultAsync(x => x.Id == vm.BatchId);

            if (batch == null)
                return NotFound();

            vm.BatchNumber = batch.BatchId;

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = await _context.BatchMonitorings
                    .AnyAsync(x => x.BatchId == vm.BatchId);

                return View(vm);
            }

            var monitoringDateTime = vm.MonitoringDateTime!.Value;

            if (monitoringDateTime < DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(vm.MonitoringDateTime),
                    "The monitoring date and time cannot be in the past.");

                ViewBag.IsEdit = await _context.BatchMonitorings
                    .AnyAsync(x => x.BatchId == vm.BatchId);

                return View(vm);
            }

            var monitoring = await _context.BatchMonitorings
                .Where(x => x.BatchId == batch.Id)
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync();

            if (monitoring != null)
            {
                // Update existing screening.
                monitoring.MonitoringDateTime = monitoringDateTime;
                monitoring.Status = "Scheduled";
                monitoring.UpdatedOn = DateTime.UtcNow;
            }
            else
            {
                // Create first screening.
                monitoring = new BatchMonitoring
                {
                    BatchId = batch.Id,
                    MonitoringDateTime = monitoringDateTime,
                    Status = "Scheduled",
                    CreatedOn = DateTime.UtcNow
                };

                _context.BatchMonitorings.Add(monitoring);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = monitoring.UpdatedOn.HasValue
                ? $"Screening updated successfully for batch {batch.BatchId}."
                : $"Screening scheduled successfully for batch {batch.BatchId}.";

            return RedirectToAction(nameof(BatchList));
        }

        [HttpGet]
        public async Task<IActionResult> SubmitReport(int monitoringId)
        {
            var monitoring = await _context.BatchMonitorings
                .AsNoTracking()
                .Include(m => m.Batch)
                .FirstOrDefaultAsync(m => m.Id == monitoringId);

            if (monitoring == null)
                return NotFound();

            if (monitoring.Status != "Scheduled")
                return BadRequest("This monitoring is not available for report submission.");

            var existingReport = await _context.BatchMonitoringReports
                .AnyAsync(r => r.BatchMonitoringId == monitoringId);

            if (existingReport)
                return Conflict("A report has already been submitted for this screening.");

            var vm = new BatchMonitoringReportVM
            {
                BatchMonitoringId = monitoring.Id,
                BatchId = monitoring.BatchId,
                BatchNumber = monitoring.Batch.BatchId,
                MonitoringDateTime = monitoring.MonitoringDateTime
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> SubmitReport(BatchMonitoringReportVM vm)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var monitoring = await _context.BatchMonitorings
                .Include(m => m.Batch)
                .FirstOrDefaultAsync(m => m.Id == vm.BatchMonitoringId);

            if (monitoring == null)
                return NotFound();

            if (monitoring.Status != "Scheduled")
                return BadRequest("This monitoring is not available for report submission.");

            if (await _context.BatchMonitoringReports
                .AnyAsync(r => r.BatchMonitoringId == vm.BatchMonitoringId))
            {
                return Conflict("A report has already been submitted for this screening.");
            }

            vm.BatchId = monitoring.BatchId;
            vm.BatchNumber = monitoring.Batch.BatchId;
            vm.MonitoringDateTime = monitoring.MonitoringDateTime;

            if (vm.MonitoringPhotos != null)
            {
                var ext = Path.GetExtension(vm.MonitoringPhotos.FileName).ToLowerInvariant();

                if (ext is not (".jpg" or ".jpeg" or ".png"))
                    ModelState.AddModelError(nameof(vm.MonitoringPhotos),
                        "Monitoring photos must be JPG, JPEG or PNG.");

                if (vm.MonitoringPhotos.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError(nameof(vm.MonitoringPhotos),
                        "Each monitoring photo must be 5 MB or smaller.");
            }

            if (vm.SupportingDocument != null)
            {
                var ext = Path.GetExtension(vm.SupportingDocument.FileName).ToLowerInvariant();

                if (ext != ".pdf")
                    ModelState.AddModelError(nameof(vm.SupportingDocument),
                        "The supporting document must be a PDF.");

                if (vm.SupportingDocument.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError(nameof(vm.SupportingDocument),
                        "The supporting document must be 5 MB or smaller.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var folder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "monitoring-reports",
                monitoring.BatchId.ToString());

            Directory.CreateDirectory(folder);

            async Task<string?> SaveFileAsync(IFormFile? file, string prefix)
            {
                if (file == null || file.Length == 0)
                    return null;

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{prefix}_{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(folder, fileName);

                await using var stream = new FileStream(fullPath, FileMode.CreateNew);
                await file.CopyToAsync(stream);

                return $"/uploads/monitoring-reports/{monitoring.BatchId}/{fileName}";
            }

            string? photoPath = null;
            string? documentPath = null;

            try
            {
                photoPath = await SaveFileAsync(vm.MonitoringPhotos, "photo");
                documentPath = await SaveFileAsync(vm.SupportingDocument, "document");

                var report = new BatchMonitoringReport
                {
                    BatchMonitoringId = monitoring.Id,
                    SubmittedByUserId = user.Id,
                    SubmittedOn = DateTime.UtcNow,

                    LabAvailable = vm.LabAvailable,
                    InternetAvailable = vm.InternetAvailable,
                    EquipmentAvailable = vm.EquipmentAvailable,
                    ClassroomAvailable = vm.ClassroomAvailable,

                    CandidateAttendanceVerified = vm.CandidateAttendanceVerified,
                    AttendanceUploadedInSystem = vm.AttendanceUploadedInSystem,
                    BiometricVerified = vm.BiometricVerified,

                    TrainerPresent = vm.TrainerPresent,
                    SessionConductedProperly = vm.SessionConductedProperly,
                    TrainingMaterialAvailable = vm.TrainingMaterialAvailable,

                    PracticalSessionConducted = vm.PracticalSessionConducted,
                    LMSUsageVerified = vm.LMSUsageVerified,
                    CandidateFeedbackCollected = vm.CandidateFeedbackCollected,

                    Remarks = vm.Remarks?.Trim(),
                    MonitoringPhotosPath = photoPath,
                    SupportingDocumentPath = documentPath
                };

                _context.BatchMonitoringReports.Add(report);

                // Mark the monitoring as completed after successful report submission.
                monitoring.Status = "Completed";
                monitoring.UpdatedOn = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Monitoring report submitted successfully for batch {monitoring.Batch.BatchId}.";

                return RedirectToAction(nameof(BatchList));
            }
            catch
            {
                // Avoid leaving uploaded files behind if saving the report fails.
                foreach (var path in new[] { photoPath, documentPath })
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        var fullPath = Path.Combine(
                            _environment.WebRootPath,
                            path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                        if (System.IO.File.Exists(fullPath))
                            System.IO.File.Delete(fullPath);
                    }
                }

                throw;
            }
        }
        [HttpGet]
        public async Task<IActionResult> ViewReport(int monitoringId)
        {
            var report = await _context.BatchMonitoringReports
                .AsNoTracking()
                .Include(r => r.BatchMonitoring)
                    .ThenInclude(m => m.Batch)
                .FirstOrDefaultAsync(r => r.BatchMonitoringId == monitoringId);

            if (report == null)
                return NotFound("Monitoring report not found.");

            return View(report);
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "MonitoringTeam")]
        //public async Task<IActionResult> ApproveBatch(int batchId)
        //{
        //    var userId = _userManager.GetUserId(User);

        //    if (string.IsNullOrEmpty(userId))
        //        return Forbid();

        //    var batch = await _context.Batches.FindAsync(batchId);

        //    if (batch == null)
        //        return NotFound();

        //    if (!string.Equals(
        //        batch.Status?.Trim(),
        //        "Pending",
        //        StringComparison.OrdinalIgnoreCase))
        //    {
        //        TempData["Error"] = "Only pending batches can be approved.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    batch.Status = "Approved";
        //    batch.ApprovedByUserId = userId;
        //    batch.ApprovedOn = DateTime.UtcNow;
        //    batch.ApprovalRemarks = null;

        //    await _context.SaveChangesAsync();

        //    TempData["Success"] = "Batch approved successfully.";

        //    return RedirectToAction(nameof(Index));
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "MonitoringTeam")]
        public async Task<IActionResult> ApproveBatch(int batchId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var batch = await _context.Batches
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
                return NotFound();

            if (!string.Equals(
                batch.Status?.Trim(),
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only pending batches can be approved.";
                return RedirectToAction(nameof(Index));
            }

            // Candidate enrollment email template.
            var candidateTemplatePath = Path.Combine(
                _environment.WebRootPath,
                "EmailTemplates",
                "CandidateBatchEnroll.html");

            // TP approval email template.
            var tpTemplatePath = Path.Combine(
                _environment.WebRootPath,
                "EmailTemplates",
                "TPBatchApproved.html");

            if (!System.IO.File.Exists(candidateTemplatePath))
            {
                TempData["Error"] =
                    "The candidate email template could not be found.";

                return RedirectToAction(nameof(Index));
            }

            if (!System.IO.File.Exists(tpTemplatePath))
            {
                TempData["Error"] =
                    "The TP approval email template could not be found.";

                return RedirectToAction(nameof(Index));
            }

            var enrollments = await _context.BatchCandidates
                .Include(bc => bc.Candidate)
                .Where(bc => bc.BatchRecordId == batch.Id)
                .ToListAsync();

            var candidateEmails = new List<(
                string Email,
                Dictionary<string, string> Replacements)>();

            string? tpEmail = null;
            Dictionary<string, string>? tpReplacements = null;

            try
            {
                foreach (var enrollment in enrollments)
                {
                    var candidate = enrollment.Candidate;

                    if (candidate == null)
                        continue;

                    var email = candidate.Email?
                        .Trim()
                        .ToLowerInvariant();

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        throw new InvalidOperationException(
                            $"Candidate '{candidate.CandidateName}' has no email address.");
                    }

                    AppUser? appUser = null;
                    var isNewUser = false;
                    var temporaryPassword = string.Empty;

                    // Resolve an already-linked Identity account.
                    if (!string.IsNullOrWhiteSpace(candidate.ApplicationUserId))
                    {
                        appUser = await _userManager.FindByIdAsync(
                            candidate.ApplicationUserId);

                        if (appUser == null)
                        {
                            throw new InvalidOperationException(
                                $"The linked login account for {email} was not found.");
                        }

                        if (!string.Equals(
                            appUser.Email,
                            email,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"The email for {candidate.CandidateName} does not match the linked account.");
                        }
                    }
                    else
                    {
                        // Reuse an existing account if the email already exists.
                        appUser = await _userManager.FindByEmailAsync(email);
                    }

                    // Create a login only if no account exists.
                    if (appUser == null)
                    {
                        temporaryPassword = GenerateTemporaryPassword();

                        appUser = new AppUser
                        {
                            Name=candidate.CandidateName,
                            UserName = email,
                            Email = email,
                            EmailConfirmed = false,
                            Password=temporaryPassword
                        };

                        var createResult = await _userManager.CreateAsync(
                            appUser,
                            temporaryPassword);

                        if (!createResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                $"Could not create login for {email}: " +
                                string.Join(", ",
                                    createResult.Errors.Select(e => e.Description)));
                        }

                        isNewUser = true;
                    }

                    // Ensure Candidate role is assigned.
                    if (!await _userManager.IsInRoleAsync(appUser, "Candidate"))
                    {
                        var roleResult = await _userManager.AddToRoleAsync(
                            appUser,
                            "Candidate");

                        if (!roleResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                $"Could not assign Candidate role to {email}: " +
                                string.Join(", ",
                                    roleResult.Errors.Select(e => e.Description)));
                        }
                    }

                    // Link the candidate profile to the login account.
                    candidate.ApplicationUserId = appUser.Id;
                    enrollment.Status = "Enrolled";

                    var replacements = new Dictionary<string, string>
                    {
                        ["Name"] = candidate.CandidateName ?? "Candidate",
                        ["UserName"] = email,
                        ["Password"] = isNewUser
                            ? temporaryPassword
                            : "Use your existing password",
                        ["BaseUrl"] = _configuration["AbsPath"]
                            ?? "https://erp.tsscindia.com/",
                        ["BatchId"] = batch.BatchId,
                        ["StartDate"] = batch.StartDate.ToString("dd MMM yyyy"),
                        ["EndDate"] = batch.EndDate.ToString("dd MMM yyyy")
                    };

                    candidateEmails.Add((email, replacements));
                }

                // Approve the batch.
                batch.Status = "Approved";
                batch.ApprovedByUserId = userId;
                batch.ApprovedOn = DateTime.UtcNow;
                batch.ApprovalRemarks = null;

                await _context.SaveChangesAsync();

                // Resolve the TP's registered email address.
                var tpRegistration = await _context.TPRegistrations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == batch.TPId);

                if (tpRegistration != null &&
                    !string.IsNullOrWhiteSpace(tpRegistration.ApplicationUserId))
                {
                    var tpUser = await _userManager.FindByIdAsync(
                        tpRegistration.ApplicationUserId);

                    if (!string.IsNullOrWhiteSpace(tpUser?.Email))
                    {
                        tpEmail = tpUser.Email;

                        tpReplacements = new Dictionary<string, string>
                        {
                            ["Name"] = tpUser.UserName ?? "Training Partner",
                            ["BatchId"] = batch.BatchId,
                            ["Status"] = "Approved",
                            ["StartDate"] = batch.StartDate.ToString("dd MMM yyyy"),
                            ["EndDate"] = batch.EndDate.ToString("dd MMM yyyy"),
                            ["BaseUrl"] = _configuration["AbsPath"]
                                ?? "https://erp.tsscindia.com/"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // Replace with _logger.LogError(ex, ...) in production.
                TempData["Error"] =
                    $"Batch approval processing failed: {ex.Message}";

                return RedirectToAction(nameof(Index));
            }

            // Send TP approval notification.
            var tpEmailSent = false;

            if (!string.IsNullOrWhiteSpace(tpEmail) &&
                tpReplacements != null)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        tpEmail,
                        "Batch Approval Notification",
                        tpTemplatePath,
                        tpReplacements);

                    tpEmailSent = true;
                }
                catch (Exception)
                {
                    // Log the email failure. The batch remains approved.
                }
            }

            // Send candidate enrollment notifications.
            var sentCount = 0;
            var failedCount = 0;

            foreach (var item in candidateEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        item.Email,
                        $"TSSC Batch Details - {batch.BatchId}",
                        candidateTemplatePath,
                        item.Replacements);

                    sentCount++;
                }
                catch (Exception)
                {
                    failedCount++;
                    // Log the email failure.
                }
            }

            var tpMessage = tpEmailSent
                ? "TP notification sent."
                : "TP notification could not be sent.";

            TempData["Success"] =
                $"Batch approved successfully. {tpMessage} " +
                $"{sentCount} candidate email(s) sent; " +
                $"{failedCount} failed.";

            return RedirectToAction(nameof(BatchList));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "MonitoringTeam")]
        public async Task<IActionResult> RejectBatch(int batchId, string? remarks)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["Error"] = "Rejection remarks are required.";
                return RedirectToAction(nameof(Index));
            }

            remarks = remarks.Trim();

            if (remarks.Length > 1000)
            {
                TempData["Error"] = "Rejection remarks cannot exceed 1000 characters.";
                return RedirectToAction(nameof(Index));
            }

            var batch = await _context.Batches.FindAsync(batchId);

            if (batch == null)
                return NotFound();

            if (!string.Equals(
                batch.Status?.Trim(),
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only pending batches can be rejected.";
                return RedirectToAction(nameof(BatchList));
            }

            batch.Status = "Rejected";
            batch.ApprovedByUserId = userId;
            batch.ApprovedOn = DateTime.UtcNow;
            batch.ApprovalRemarks = remarks;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Batch rejected successfully.";

            return RedirectToAction(nameof(BatchList));
        }
        private static string GenerateTemporaryPassword()
        {
            const string lowercase = "abcdefghijkmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "0123456789";

            var random = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);

            var password = new char[8];

            password[0] = uppercase[random[0] % uppercase.Length];
            password[1] = lowercase[random[1] % lowercase.Length];
            password[2] = digits[random[2] % digits.Length];

            const string allChars = lowercase + uppercase + digits;

            for (int i = 3; i < 8; i++)
            {
                password[i] = allChars[random[i] % allChars.Length];
            }

            // Shuffle the characters
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random[i] % (i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var batch = await _context.Batches
                .AsNoTracking()
                .Include(x => x.Candidates)
                .FirstOrDefaultAsync(x =>
                    x.Id == id  );

            if (batch == null)
                return NotFound();

            ViewBag.SchemeName = await _context.Scheme
                .Where(x => x.SchemeId == batch.SchemeId)
                .Select(x => x.SchemeName)
                .FirstOrDefaultAsync();

            ViewBag.ProjectName = await _context.Project
                .Where(x => x.Id == batch.ProjectId)
                .Select(x => x.ProjectName)
                .FirstOrDefaultAsync();

            ViewBag.CenterName = await _context.TrainingCenter
                .Where(x => x.Id == batch.TrainingCenterId)
                .Select(x => x.TrainingCenterName)
                .FirstOrDefaultAsync();

            ViewBag.JobRoleName = await _context.JobRoles
                .Where(x => x.Id == batch.JobRoleId)
                .Select(x => x.JobRoleTitle)
                .FirstOrDefaultAsync();

            ViewBag.TrainerName = await _context.TrainerRegistration
                .Where(x => x.Id == batch.TrainerRegistrationId)
                .Select(x => x.CandidateName)
                .FirstOrDefaultAsync();

            // Get the candidates currently enrolled in this batch.
            ViewBag.BatchCandidates = await _context.BatchCandidates
            .AsNoTracking()
            .Where(x => x.BatchRecordId == batch.Id)
            .Include(x => x.Candidate)
            .Select(x => new
            {
                x.Candidate.Id,
                x.Candidate.CandidateName,
                x.Candidate.FatherName,
                x.Candidate.AadharNo,
                x.Candidate.DOB,
                x.Candidate.MobileNumber,
                x.Candidate.Email,
                x.EnrollmentDate,
                x.Status
            })
            .ToListAsync();
            return View(batch);
        }
    }
}