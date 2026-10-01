using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;

namespace TSSC.Unified.Areas.Agency.Controllers
{
    [Area("Agency")]
    [Authorize]
    public class AgencyController :Controller
    {

        private readonly ILogger<AgencyController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        private readonly EmailService _emailService;
        public AgencyController(
            ILogger<AgencyController> logger,
            AppdbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment,
            EmailService emailService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
            _emailService = emailService;
        }
        [HttpGet]
        public async Task<IActionResult> AssignedBatches()
        {
            var batches = await _context.BatchMaster
                .AsNoTracking()
                .Include(x => x.JobRole)
                .Include(x => x.AssessmentAgency)
                .Where(x =>
                    x.AssessmentAgencyId != null &&
                    x.AgencyApprovalStatus == "Approved" &&
                    x.AgencySent)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(batches);
        }
        [HttpGet]
        public async Task<IActionResult> ViewBatch(int id)
        {
            var batch = await _context.BatchMaster
                .AsNoTracking()
                .Include(x => x.JobRole)
                .Include(x => x.AssessmentAgency)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.AgencyApprovalStatus == "Approved" &&
                    x.AgencySent);

            if (batch == null)
            {
                TempData["error"] = "Batch not found.";
                return RedirectToAction(nameof(AssignedBatches));
            }

            var trainers = await _context.BatchMasterTrainers
                .AsNoTracking()
                .Include(x => x.TrainerRegistration)
                .Where(x => x.BatchId == id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
           
            ViewBag.Batch = batch;

            return View(trainers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResult(int batchId,IFormFile resultFile)
        {

            var batch = await _context.BatchMaster
                .FirstOrDefaultAsync(x =>
                    x.Id == batchId &&
                    x.AssessmentAgencyId != null &&
                    x.AgencyApprovalStatus == "Approved" &&
                    x.AgencySent);

            if (batch == null)
            {
                TempData["error"] = "Invalid or unauthorized batch.";
                return RedirectToAction(nameof(AssignedBatches));
            }

            if (resultFile == null || resultFile.Length == 0)
            {
                TempData["error"] = "Please select an Excel result file.";
                return RedirectToAction(nameof(AssignedBatches));
            }


            const long maxFileSize = 10 * 1024 * 1024;

            if (resultFile.Length > maxFileSize)
            {
                TempData["error"] =
                    "Result file size must not exceed 10 MB.";

                return RedirectToAction(nameof(AssignedBatches));
            }


            var extension =
                Path.GetExtension(resultFile.FileName)
                    .ToLowerInvariant();

            if (extension != ".xlsx" && extension != ".xls")
            {
                TempData["error"] =
                    "Only Excel files (.xlsx, .xls) are allowed.";

                return RedirectToAction(nameof(AssignedBatches));
            }


            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {

                var folderName =
                    !string.IsNullOrWhiteSpace(batch.BatchCode)
                        ? batch.BatchCode
                        : $"Batch-{batch.Id}";

                foreach (var invalidChar in
                         Path.GetInvalidFileNameChars())
                {
                    folderName = folderName.Replace(
                        invalidChar.ToString(),
                        string.Empty);
                }

                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "assessment-results",
                    folderName);

                Directory.CreateDirectory(uploadFolder);

                var originalName =
                    Path.GetFileNameWithoutExtension(
                        resultFile.FileName);

                foreach (var invalidChar in
                         Path.GetInvalidFileNameChars())
                {
                    originalName = originalName.Replace(
                        invalidChar.ToString(),
                        string.Empty);
                }

                if (string.IsNullOrWhiteSpace(originalName))
                {
                    originalName = "AssessmentResult";
                }

                var fileName =
                    $"{originalName}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";

                var filePath =
                    Path.Combine(uploadFolder, fileName);

                await using (var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await resultFile.CopyToAsync(stream);
                }

                batch.ResultUploaded = true;
                batch.ResultUploadedDate = DateTime.Now;
                batch.ResultFileName = fileName;

                batch.ResultFilePath =
                    Path.Combine(
                        "uploads",
                        "assessment-results",
                        folderName,
                        fileName)
                    .Replace("\\", "/");

                batch.UpdatedDate = DateTime.Now;
                batch.UpdatedBy = _userManager.GetUserId(User);

                var batchTrainers = await _context.BatchMasterTrainers
                    .Include(x => x.TrainerRegistration)
                    .Where(x => x.BatchId == batchId)
                    .ToListAsync();

                foreach (var batchTrainer in batchTrainers)
                {
                    batchTrainer.ResultUploaded = true;
                    batchTrainer.ResultUploadedDate = DateTime.Now;
                }


                await _context.SaveChangesAsync();

                foreach (var batchTrainer in batchTrainers)
                {
                    var trainer =
                        batchTrainer.TrainerRegistration;

                    if (trainer == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(trainer.Email))
                        continue;
                    if (batchTrainer.ResultEmailSent)
                        continue;


                    try
                    {
                        var templatePath = Path.Combine(
                            _environment.WebRootPath,
                            "EmailTemplates",
                            "TrainerResultDeclared.html");


                        if (!System.IO.File.Exists(templatePath))
                        {
                            _logger.LogError(
                                "Result email template not found: {Path}",
                                templatePath);

                            continue;
                        }


                        var baseUrl =
                            $"{Request.Scheme}://{Request.Host}";


                        var replacements =
                            new Dictionary<string, string>
                            {
                                ["BaseUrl"] = baseUrl,

                                ["TrainerName"] =
                                    trainer.CandidateName ?? string.Empty,

                                ["RegistrationNo"] =
                                    trainer.RegistrationNo ?? string.Empty,

                                ["BatchCode"] =
                                    batch.BatchCode ?? string.Empty,

                                ["BatchName"] =
                                    batch.BatchName ?? string.Empty,

                                ["PortalUrl"] =
                                    $"{baseUrl}/Trainer/Trainer/ApplicationStatus"
                            };


                        var result =
                            await _emailService.SendEmailAsync(
                                trainer.Email.Trim(),
                                "Result Declared - Certificate Request - TSSC",
                                templatePath,
                                replacements);


                        if (string.Equals(
                            result?.Trim(),
                            "True",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            batchTrainer.ResultEmailSent = true;
                            batchTrainer.ResultEmailSentDate =
                                DateTime.Now;
                        }
                        else
                        {
                            _logger.LogError(
                                "Result email failed for TrainerId {TrainerId}. Response: {Response}",
                                trainer.Id,
                                result);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(
                            emailEx,
                            "Error sending result email to TrainerId {TrainerId}",
                            trainer.Id);
                    }
                }


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                TempData["msg"] =
                    $"Result uploaded successfully for batch {batch.BatchCode ?? batch.BatchName}.";

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Error uploading result for BatchId {BatchId}",
                    batchId);

                TempData["error"] =
                    "Unable to upload result file.";
            }


            return RedirectToAction(nameof(AssignedBatches));
        }
    }
}
