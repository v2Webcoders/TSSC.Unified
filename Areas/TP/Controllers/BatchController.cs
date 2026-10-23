using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
using iText.Commons.Internal.Runtime;
using Microsoft.AspNetCore.Identity;
using QUIZAPP.Models;
using QUIZAPP.Services;

namespace TSSC.Unified.Areas.TP.Controllers
{
    [Area("TP")]
    [Authorize(Roles = "TrainingPartner")]
    public class BatchController : Controller
    {
        private readonly AppdbContext _context;
        private readonly EmailService _emailService;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public BatchController(
            AppdbContext context,
            EmailService emailService,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _environment = environment;
        }

        // Replace these sample names and IDs with the real static project list.
        // IDs must match TrainingCenter.ProjectId.
        private static readonly List<SelectListItem> ProjectOptions = new()
        {
            new() { Value = "1", Text = "Project A" },
            new() { Value = "2", Text = "Project B" }
        };

        // This assumes a numeric TPId claim is present after TP login.
        private int? GetCurrentTPId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return _context.TPRegistrations
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => (int?)x.Id)
                .FirstOrDefault();
        }



        private async Task LoadDropdownsAsync(
            BatchVM vm,
            bool loadCenters = true,
            bool loadTrainers = true)
        {
            // Scheme dropdown
            vm.Schemes = await _context.Scheme
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SchemeName)
                .Select(x => new SelectListItem
                {
                    Value = x.SchemeId.ToString(),
                    Text = x.SchemeName
                })
                .ToListAsync();

            // Project dropdown
            vm.Projects = await _context.Project
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProjectName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.ProjectName
                })
                .ToListAsync();

            // Job Role dropdown
            vm.JobRoles = await _context.JobRoles
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.JobRoleTitle)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.JobRoleTitle
                })
                .ToListAsync();

            // Always initialize the lists
            vm.Centers = new List<SelectListItem>();
            vm.Trainers = new List<SelectListItem>();

            // Always initialize the list
            vm.Centers = new List<SelectListItem>();

            // Load centers only when all three parent dropdowns are selected

            if (loadCenters &&
                vm.SchemeId > 0 &&
                vm.ProjectId > 0 &&
                vm.JobRoleId > 0)
            {
                vm.Centers = await _context.TrainingCenter
                    .AsNoTracking()
                    .OrderBy(x => x.TrainingCenterName)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.TrainingCenterName +
                               " (" + x.TrainingCenterCode + ")",
                        Selected = x.Id == vm.TrainingCenterId
                    })
                    .ToListAsync();
            }

            // Trainer dropdown
            var tpId = GetCurrentTPId();

            if (loadTrainers && tpId.HasValue)
            {
                vm.Trainers = await _context.TrainerRegistration
                    .AsNoTracking()
                    .Where(x =>
                        x.TPId == tpId.Value &&
                        x.IsActive)
                    .OrderBy(x => x.CandidateName)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.CandidateName,
                        Selected = x.Id == vm.TrainerRegistrationId
                    })
                    .ToListAsync();
            }
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var tpId = GetCurrentTPId();

            if (!tpId.HasValue)
                return Forbid();

            var vm = new BatchVM();

            await LoadDropdownsAsync(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BatchVM vm)
        {
            var tpId = GetCurrentTPId();

            if (!tpId.HasValue)
                return Forbid();

            // 1. Validate Batch ID
            var batchId = vm.BatchId?.Trim();

            if (string.IsNullOrWhiteSpace(batchId))
            {
                ModelState.AddModelError(
                    nameof(vm.BatchId),
                    "Batch ID is required.");
            }
            else if (await _context.Batches.AnyAsync(
                x => x.BatchId == batchId))
            {
                ModelState.AddModelError(
                    nameof(vm.BatchId),
                    "This Batch ID already exists.");
            }

            // 2. Validate dates
            if (vm.EndDate.Date < vm.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(vm.EndDate),
                    "End Date cannot be earlier than Start Date.");
            }

            // 3. Validate Excel file
            var validExcelFile = false;

            if (vm.CandidateFile == null || vm.CandidateFile.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(vm.CandidateFile),
                    "Please upload an Excel file containing candidates.");
            }
            else
            {
                var extension = Path.GetExtension(vm.CandidateFile.FileName);

                if (!string.Equals(
                    extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        nameof(vm.CandidateFile),
                        "Only .xlsx Excel files are supported.");
                }
                else if (vm.CandidateFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(vm.CandidateFile),
                        "The Excel file cannot exceed 5 MB.");
                }
                else
                {
                    validExcelFile = true;
                }
            }

            // 4. Validate active Scheme
            var schemeExists = await _context.Scheme.AnyAsync(x =>
                x.SchemeId == vm.SchemeId &&
                x.IsActive);

            if (!schemeExists)
            {
                ModelState.AddModelError(
                    nameof(vm.SchemeId),
                    "Select a valid active scheme.");
            }

            // 5. Validate Project
            var projectExists = ProjectOptions.Any(x =>
                x.Value == vm.ProjectId.ToString());

            if (!projectExists)
            {
                ModelState.AddModelError(
                    nameof(vm.ProjectId),
                    "Select a valid project.");
            }

            // 6. Validate active Job Role
            var jobRoleExists = await _context.JobRoles.AnyAsync(x =>
                x.Id == vm.JobRoleId &&
                x.IsActive);

            if (!jobRoleExists)
            {
                ModelState.AddModelError(
                    nameof(vm.JobRoleId),
                    "Select a valid active job role.");
            }

            // 7. Validate Trainer belongs to the current TP
            var trainerExists = await _context.TrainerRegistration.AnyAsync(x =>
                x.Id == vm.TrainerRegistrationId &&
                x.TPId == tpId.Value &&
                x.IsActive);

            if (!trainerExists)
            {
                ModelState.AddModelError(
                    nameof(vm.TrainerRegistrationId),
                    "Select an active trainer registered under your TP account.");
            }

            // 8. Read candidates from Excel
            List<CandidateExcelRow> importedCandidates = new();

            if (validExcelFile && vm.CandidateFile != null)
            {
                try
                {
                    importedCandidates =
                        await ReadCandidatesAsync(vm.CandidateFile);

                    if (importedCandidates.Count == 0)
                    {
                        ModelState.AddModelError(
                            nameof(vm.CandidateFile),
                            "No valid candidates were found in the Excel file.");
                    }

                    for (int i = 0; i < importedCandidates.Count; i++)
                    {
                        var candidate = importedCandidates[i];

                        if (string.IsNullOrWhiteSpace(candidate.CandidateName))
                        {
                            ModelState.AddModelError(
                                nameof(vm.CandidateFile),
                                $"Candidate name is missing in Excel row {i + 2}.");
                        }

                        if (string.IsNullOrWhiteSpace(candidate.MobileNumber))
                        {
                            ModelState.AddModelError(
                                nameof(vm.CandidateFile),
                                $"Mobile number is missing in Excel row {i + 2}.");
                        }

                        if (string.IsNullOrWhiteSpace(candidate.Email) ||
                            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                                .IsValid(candidate.Email.Trim()))
                        {
                            ModelState.AddModelError(
                                nameof(vm.CandidateFile),
                                $"A valid email address is required in Excel row {i + 2}.");
                        }
                    }

                    // Prevent duplicate candidates in the same upload.
                    var duplicateEmails = importedCandidates
                        .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                        .GroupBy(c => c.Email!.Trim(),
                            StringComparer.OrdinalIgnoreCase)
                        .Any(g => g.Count() > 1);

                    var duplicateMobiles = importedCandidates
                        .Where(c => !string.IsNullOrWhiteSpace(c.MobileNumber))
                        .GroupBy(c => c.MobileNumber.Trim())
                        .Any(g => g.Count() > 1);

                    if (duplicateEmails || duplicateMobiles)
                    {
                        ModelState.AddModelError(
                            nameof(vm.CandidateFile),
                            "The Excel file contains duplicate candidate email addresses or mobile numbers.");
                    }
                }
                catch (InvalidDataException ex)
                {
                    ModelState.AddModelError(
                        nameof(vm.CandidateFile),
                        ex.Message);
                }
                catch (Exception)
                {
                    ModelState.AddModelError(
                        nameof(vm.CandidateFile),
                        "The Excel file could not be read. Please check its format.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            // Prepare email template path
            var templatePath = Path.Combine(
                _environment.WebRootPath,
                "EmailTemplates",
                "CandidateBatchEnroll.html");

            if (!System.IO.File.Exists(templatePath))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The candidate email template could not be found.");

                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            // Ensure the Candidate role exists.
            if (!await _roleManager.RoleExistsAsync("Candidate"))
            {
                var roleResult = await _roleManager.CreateAsync(
                    new IdentityRole("Candidate"));

                if (!roleResult.Succeeded)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "The Candidate role could not be created.");

                    await LoadDropdownsAsync(vm);
                    return View(vm);
                }
            }

            var pendingEmails = new List<(
                string Email,
                Dictionary<string, string> Replacements)>();

            // Used to compensate for newly created Identity users
            // if the batch database transaction fails.
            var newlyCreatedUsers = new List<AppUser>();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            //try
            {
                var now = DateTime.UtcNow;

                // TABLE 1: Create batch
                var batch = new Batch
                {
                    BatchId = batchId!,
                    SchemeId = vm.SchemeId,
                    ProjectId = vm.ProjectId,
                    TrainingCenterId = vm.TrainingCenterId,
                    JobRoleId = vm.JobRoleId,
                    TrainerRegistrationId = vm.TrainerRegistrationId,
                    StartDate = vm.StartDate.Date,
                    EndDate = vm.EndDate.Date,
                    TPId = tpId.Value,
                    Status = "Pending",
                    CreatedOn = now
                };

                _context.Batches.Add(batch);

                // Generate Batch primary key
                await _context.SaveChangesAsync();

                // TABLE 2: Find or create candidates
                // Identity: Create or reuse candidate login
                // TABLE 3: Create batch enrollments
                foreach (var imported in importedCandidates)
                {
                    var email = imported.Email!.Trim().ToLowerInvariant();
                    var mobile = imported.MobileNumber.Trim();

                    // Match candidate by email OR mobile.
                    var candidate = await _context.Candidates
                        .FirstOrDefaultAsync(c =>
                            (c.Email != null &&
                             c.Email.ToLower() == email)
                            ||
                            c.MobileNumber == mobile);

                    if (candidate != null &&
                        !string.IsNullOrWhiteSpace(candidate.Email) &&
                        !string.Equals(
                            candidate.Email.Trim(),
                            email,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"The mobile number {mobile} already belongs to a candidate with a different email address.");
                    }

                    var isNewCandidate = candidate == null;

                    if (candidate == null)
                    {
                        candidate = new Candidate
                        {
                            CandidateName = imported.CandidateName.Trim(),
                            MobileNumber = mobile,
                            Email = email,
                            CreatedOn = now
                        };
                    }
                    else if (string.IsNullOrWhiteSpace(candidate.Email))
                    {
                        candidate.Email = email;
                    }

                    // Find an existing Identity account.
                    AppUser? user = null;

                    if (!string.IsNullOrWhiteSpace(candidate.ApplicationUserId))
                    {
                        user = await _userManager.FindByIdAsync(
                            candidate.ApplicationUserId);

                        if (user == null)
                        {
                            throw new InvalidOperationException(
                                $"The linked login account for {email} was not found.");
                        }

                        if (!string.Equals(
                            user.Email, email,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"The email for candidate {candidate.CandidateName} does not match the linked login account.");
                        }
                    }
                    else
                    {
                        user = await _userManager.FindByEmailAsync(email);
                    }

                    var isNewUser = user == null;
                    var temporaryPassword = string.Empty;

                    if (user == null)
                    {
                        user = new AppUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = false
                        };

                        temporaryPassword = GenerateTemporaryPassword();

                        var createUserResult = await _userManager.CreateAsync(
                            user, temporaryPassword);

                        if (!createUserResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                $"Could not create login for {email}: " +
                                string.Join(
                                    ", ",
                                    createUserResult.Errors.Select(e => e.Description)));
                        }

                        newlyCreatedUsers.Add(user);

                        var roleResult = await _userManager.AddToRoleAsync(
                            user, "Candidate");

                        if (!roleResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                $"Could not assign Candidate role to {email}: " +
                                string.Join(
                                    ", ",
                                    roleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else if (!await _userManager.IsInRoleAsync(user, "Candidate"))
                    {
                        var roleResult = await _userManager.AddToRoleAsync(
                            user, "Candidate");

                        if (!roleResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                $"Could not assign Candidate role to {email}.");
                        }
                    }

                    candidate.ApplicationUserId = user.Id;

                    if (isNewCandidate)
                    {
                        _context.Candidates.Add(candidate);
                    }

                    // Save candidate to generate CandidateId if new.
                    await _context.SaveChangesAsync();

                    // Avoid duplicate enrollment if the candidate is already
                    // enrolled in this batch.
                    var alreadyEnrolled = await _context.BatchCandidates.AnyAsync(
                        x => x.BatchRecordId == batch.Id &&
                             x.CandidateId == candidate.Id);

                    if (!alreadyEnrolled)
                    {
                        _context.BatchCandidates.Add(new BatchCandidate
                        {
                            BatchRecordId = batch.Id,
                            CandidateId = candidate.Id,
                            EnrollmentDate = now,
                            CreatedOn = now,
                            Status = "Enrolled"
                        });
                    }

                    // Prepare email using your existing HTML template.
                    // A password is sent only for newly created accounts.
                    var replacements = new Dictionary<string, string>
                    {
                        ["Name"] = candidate.CandidateName,
                        ["UserName"] = user.Email ?? email,
                        ["Password"] = isNewUser
                            ? temporaryPassword
                            : "Use your existing password",
                        ["BaseUrl"] = _configuration["BaseUrl"]
                            ?? "https://erp.tsscindia.com/",
                        ["BatchId"] = batch.BatchId,
                        ["StartDate"] = batch.StartDate.ToString("dd MMM yyyy"),
                        ["EndDate"] = batch.EndDate.ToString("dd MMM yyyy")
                    };

                    pendingEmails.Add((email, replacements));
                }

                // Save enrollment records.
                await _context.SaveChangesAsync();

                // All three application tables have saved successfully.
                await transaction.CommitAsync();

                // Send notifications only after successful database commit.
                int emailsSent = 0;
                int emailFailures = 0;

                foreach (var pending in pendingEmails)
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            pending.Email,
                            $"TSSC Batch Details - {batch.BatchId}",
                            templatePath,
                            pending.Replacements);

                        emailsSent++;
                    }
                    catch (Exception ex)
                    {
                        emailFailures++;

                        // Replace with your ILogger logging in production.
                        // Do not log passwords or the full replacement dictionary.
                    }
                }

                TempData["SuccessMessage"] =
                    $"Batch {batch.BatchId} created successfully. " +
                    $"Candidates processed: {importedCandidates.Count}. " +
                    $"Emails sent: {emailsSent}. " +
                    $"Email failures: {emailFailures}.";

                return RedirectToAction(nameof(Index));
            }
            //catch (Exception ex)
            //{
            //    try
            //    {
            //        await transaction.RollbackAsync();
            //    }
            //    catch
            //    {
            //        // Preserve the original failure.
            //    }

            //    _context.ChangeTracker.Clear();

            //    // Compensate for Identity accounts created during this operation.
            //    // Existing accounts are never deleted here.
            //    foreach (var newUser in newlyCreatedUsers)
            //    {
            //        try
            //        {
            //            await _userManager.DeleteAsync(newUser);
            //        }
            //        catch
            //        {
            //            // Log cleanup failures and reconcile manually if necessary.
            //        }
            //    }

            //    // Log ex using ILogger; avoid showing database details to users.
            //    ModelState.AddModelError(
            //        string.Empty,
            //        ex.Message);

            //    await LoadDropdownsAsync(vm);
            //    return View(vm);
            //}
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
        public async Task<IActionResult> Index()
        {
            var tpId = GetCurrentTPId();

            var batches = await _context.Batches
                .AsNoTracking()
                .Where(x => x.TPId == tpId)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new BatchIndexVM
                {
                    Id = x.Id,
                    BatchId = x.BatchId,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Status = x.Status,
                    CreatedOn = x.CreatedOn,

                    CandidateCount = x.Candidates.Count(),
                    ProjectName=x.ProjectId.ToString(),
                    SchemeName = _context.Scheme
                        .Where(s => s.SchemeId == x.SchemeId)
                        .Select(s => s.SchemeName)
                        .FirstOrDefault(),

                    CenterName = _context.TrainingCenter
                        .Where(c => c.Id == x.TrainingCenterId)
                        .Select(c => c.TrainingCenterName)
                        .FirstOrDefault(),

                    JobRoleName = _context.JobRoles
                        .Where(j => j.Id == x.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault(),

                    TrainerName = _context.TrainerRegistration
                        .Where(t => t.Id == x.TrainerRegistrationId)
                        .Select(t => t.CandidateName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(batches);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var tpId = GetCurrentTPId();

            var batch = await _context.Batches
                .AsNoTracking()
                .Include(x => x.Candidates)
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.TPId == tpId);

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
                    x.Candidate.MobileNumber,
                    x.Candidate.Email,
                    x.EnrollmentDate,
                    x.Status
                })
                .ToListAsync();

            return View(batch);
        }

        private static Task<List<CandidateExcelRow>> ReadCandidatesAsync(
            IFormFile file)
        {
            var candidates = new List<CandidateExcelRow>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null || worksheet.FirstRowUsed() == null)
                throw new InvalidDataException("The Excel worksheet is empty.");

            var headerRow = worksheet.FirstRowUsed().RowNumber();
            var lastColumn = worksheet.Row(headerRow).LastCellUsed()?.Address.ColumnNumber ?? 0;

            int nameColumn = 0, mobileColumn = 0, emailColumn = 0;

            for (int column = 1; column <= lastColumn; column++)
            {
                var header = worksheet.Cell(headerRow, column)
                    .GetString().Trim();

                if (header.Equals("CandidateName", StringComparison.OrdinalIgnoreCase) ||
                    header.Equals("Candidate Name", StringComparison.OrdinalIgnoreCase))
                    nameColumn = column;
                else if (header.Equals("MobileNumber", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("Mobile", StringComparison.OrdinalIgnoreCase))
                    mobileColumn = column;
                else if (header.Equals("Email", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("EmailAddress", StringComparison.OrdinalIgnoreCase))
                    emailColumn = column;
            }

            if (nameColumn == 0)
                throw new InvalidDataException(
                    "Excel must contain a CandidateName or Candidate Name column.");

            if (mobileColumn == 0)
                throw new InvalidDataException(
                    "Excel must contain a MobileNumber or Mobile column.");

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;

            for (int row = headerRow + 1; row <= lastRow; row++)
            {
                var name = worksheet.Cell(row, nameColumn).GetString().Trim();
                var mobile = worksheet.Cell(row, mobileColumn).GetString().Trim();
                var email = emailColumn > 0
                    ? worksheet.Cell(row, emailColumn).GetString().Trim()
                    : null;

                if (string.IsNullOrWhiteSpace(name) &&
                    string.IsNullOrWhiteSpace(mobile) &&
                    string.IsNullOrWhiteSpace(email))
                    continue;

                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidDataException(
                        $"Candidate name is missing at Excel row {row}.");

                if (string.IsNullOrWhiteSpace(mobile))
                    throw new InvalidDataException(
                        $"Mobile number is missing at Excel row {row}.");

                if (mobile.Length > 20)
                    throw new InvalidDataException(
                        $"Mobile number is too long at Excel row {row}.");

                if (!string.IsNullOrWhiteSpace(email) &&
                    !new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                        .IsValid(email))
                {
                    throw new InvalidDataException(
                        $"Invalid email address at Excel row {row}.");
                }

                candidates.Add(new CandidateExcelRow
                {
                    CandidateName = name,
                    MobileNumber = mobile,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email
                });
            }

            return Task.FromResult(candidates);
        }

        [HttpGet]
        public async Task<IActionResult> GetCenters(
        int schemeId,
        int projectId,
        int jobRoleId)
        {
            if (schemeId <= 0 || projectId <= 0 || jobRoleId <= 0)
                return Json(new List<object>());

            var centers = await _context.TrainingCenter
                .AsNoTracking()
                .OrderBy(x => x.TrainingCenterName)
                .Select(x => new
                {
                    value = x.Id,
                    text = x.TrainingCenterName +
                           " (" + x.TrainingCenterCode + ")"
                })
                .ToListAsync();

            return Json(centers);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var tpId = GetCurrentTPId();

            var batch = await _context.Batches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TPId == tpId);

            if (batch == null)
                return NotFound();

            var vm = new EditBatchVM
            {
                Id = batch.Id,
                BatchId = batch.BatchId,
                SchemeId = batch.SchemeId,
                ProjectId = batch.ProjectId,
                TrainingCenterId = batch.TrainingCenterId,
                JobRoleId = batch.JobRoleId,
                TrainerRegistrationId = batch.TrainerRegistrationId,
                StartDate = batch.StartDate,
                EndDate = batch.EndDate,

                // CandidateFile is optional; the user uploads it on the Edit page.
                CandidateFile = null
            };

            await LoadEditDropdownsAsync(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditBatchVM vm)
        {
            var tpId = GetCurrentTPId();

            var batch = await _context.Batches
                .FirstOrDefaultAsync(x => x.Id == vm.Id && x.TPId == tpId);

            if (batch == null)
                return NotFound();

            // Prevent duplicate Batch IDs for the same TP.
            var normalizedBatchId = vm.BatchId?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(normalizedBatchId))
            {
                var duplicateBatchId = await _context.Batches.AnyAsync(x =>
                    x.Id != vm.Id &&
                    x.TPId == tpId &&
                    x.BatchId == normalizedBatchId);

                if (duplicateBatchId)
                {
                    ModelState.AddModelError(
                        nameof(vm.BatchId),
                        "This Batch ID already exists.");
                }
            }

            // Validate selected IDs against the database.
            var schemeExists = await _context.Scheme.AnyAsync(s =>
                s.SchemeId == vm.SchemeId && s.IsActive);

            var projectExists = await _context.Project.AnyAsync(p =>
                p.Id == vm.ProjectId && p.IsActive);

            var centerExists = await _context.TrainingCenter.AnyAsync(c =>
                c.Id == vm.TrainingCenterId);

            var jobRoleExists = await _context.JobRoles.AnyAsync(j =>
                j.Id == vm.JobRoleId && j.IsActive);

            var trainerExists = await _context.TrainerRegistration.AnyAsync(t =>
                t.Id == vm.TrainerRegistrationId);

            if (!schemeExists)
                ModelState.AddModelError(nameof(vm.SchemeId),
                    "Select a valid active scheme.");

            if (!projectExists)
                ModelState.AddModelError(nameof(vm.ProjectId),
                    "Select a valid active project.");

            if (!centerExists)
                ModelState.AddModelError(nameof(vm.TrainingCenterId),
                    "Select a valid training center.");

            if (!jobRoleExists)
                ModelState.AddModelError(nameof(vm.JobRoleId),
                    "Select a valid active job role.");

            if (!trainerExists)
                ModelState.AddModelError(nameof(vm.TrainerRegistrationId),
                    "Select a valid trainer.");

            // Validate the Excel upload, if supplied.
            if (vm.CandidateFile != null && vm.CandidateFile.Length > 0)
            {
                var extension = Path.GetExtension(vm.CandidateFile.FileName);

                if (!string.Equals(extension, ".xlsx",
                        StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        nameof(vm.CandidateFile),
                        "Upload an Excel file with the .xlsx extension.");
                }

                if (vm.CandidateFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(vm.CandidateFile),
                        "The Excel file must not exceed 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadEditDropdownsAsync(vm);
                return View(vm);
            }

            // Keep batch details and candidate replacement in one transaction.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Update batch details.
                batch.BatchId = normalizedBatchId;
                batch.SchemeId = vm.SchemeId;
                batch.ProjectId = vm.ProjectId;
                batch.TrainingCenterId = vm.TrainingCenterId;
                batch.JobRoleId = vm.JobRoleId;
                batch.TrainerRegistrationId = vm.TrainerRegistrationId;
                batch.StartDate = vm.StartDate.Date;
                batch.EndDate = vm.EndDate.Date;
                batch.UpdatedOn = DateTime.UtcNow;

                var candidatesUploaded =
                    vm.CandidateFile != null && vm.CandidateFile.Length > 0;

                if (candidatesUploaded)
                {
                    var replaced = await ReplaceBatchCandidatesAsync(
                        batch,
                        vm.CandidateFile!);

                    if (!replaced)
                    {
                        await transaction.RollbackAsync();

                        await LoadEditDropdownsAsync(vm);
                        return View(vm);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = candidatesUploaded
                    ? "Batch details and candidate list updated successfully."
                    : "Batch updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the batch. Please check the data and try again.");

                await LoadEditDropdownsAsync(vm);
                return View(vm);
            }
        }

        private async Task<bool> ReplaceBatchCandidatesAsync(
        Batch batch,
        IFormFile file)
        {
            List<CandidateExcelRow> rows;

            try
            {
                rows = await ReadCandidatesAsync(file);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    nameof(EditBatchVM.CandidateFile),
                    "Unable to read the Excel file. Please check its format.");

                return false;
            }

            if (rows.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(EditBatchVM.CandidateFile),
                    "The Excel file does not contain any candidates.");

                return false;
            }

            // Normalize uploaded values.
            var candidates = rows.Select(r => new
            {
                Name = r.CandidateName?.Trim() ?? string.Empty,
                Mobile = r.MobileNumber?.Trim() ?? string.Empty,
                Email = string.IsNullOrWhiteSpace(r.Email)
                    ? null
                    : r.Email.Trim().ToLowerInvariant()
            }).ToList();

            if (candidates.Any(c =>
                string.IsNullOrWhiteSpace(c.Name) ||
                string.IsNullOrWhiteSpace(c.Mobile)))
            {
                ModelState.AddModelError(
                    nameof(EditBatchVM.CandidateFile),
                    "Every candidate must have a name and mobile number.");

                return false;
            }

            // Reject duplicate mobile numbers in the uploaded file.
            if (candidates.GroupBy(c => c.Mobile,
                    StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            {
                ModelState.AddModelError(
                    nameof(EditBatchVM.CandidateFile),
                    "The Excel file contains duplicate mobile numbers.");

                return false;
            }

            // Reject duplicate email addresses in the uploaded file.
            var emails = candidates
                .Where(c => c.Email != null)
                .Select(c => c.Email!)
                .ToList();

            if (emails.GroupBy(e => e, StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError(
                    nameof(EditBatchVM.CandidateFile),
                    "The Excel file contains duplicate email addresses.");

                return false;
            }

            // Validate email syntax when an email is supplied.
            foreach (var candidate in candidates.Where(c => c.Email != null))
            {
                try
                {
                    _ = new System.Net.Mail.MailAddress(candidate.Email!);
                }
                catch
                {
                    ModelState.AddModelError(
                        nameof(EditBatchVM.CandidateFile),
                        $"Invalid email address: {candidate.Email}");

                    return false;
                }
            }

            var mobiles = candidates
                .Select(c => c.Mobile)
                .Distinct()
                .ToList();

            // Match existing records using mobile or email.
            // ToLower() helps make email matching case-insensitive.
            var existingCandidates = await _context.Candidates
                .Where(c =>
                    mobiles.Contains(c.MobileNumber) ||
                    (c.Email != null &&
                     emails.Contains(c.Email.ToLower())))
                .ToListAsync();

            var desiredCandidateIds = new List<int>();
            var resolvedRows = new List<(int? CandidateId, string Name,
                string Mobile, string? Email)>();

            foreach (var row in candidates)
            {
                var mobileMatches = existingCandidates
                    .Where(c => string.Equals(
                        c.MobileNumber?.Trim(),
                        row.Mobile,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var emailMatches = row.Email == null
                    ? new List<Candidate>()
                    : existingCandidates
                        .Where(c => string.Equals(
                            c.Email?.Trim(),
                            row.Email,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                // Existing data must not map one uploaded row to multiple people.
                var matchedIds = mobileMatches
                    .Concat(emailMatches)
                    .Select(c => c.Id)
                    .Distinct()
                    .ToList();

                if (matchedIds.Count > 1 ||
                    mobileMatches.Count > 1 ||
                    emailMatches.Count > 1)
                {
                    ModelState.AddModelError(
                        nameof(EditBatchVM.CandidateFile),
                        $"The mobile number or email for '{row.Name}' matches conflicting candidate records.");

                    return false;
                }

                resolvedRows.Add((
                    matchedIds.Count == 1 ? matchedIds[0] : null,
                    row.Name,
                    row.Mobile,
                    row.Email));
            }

            // No candidate or enrollment changes have been made before this point.
            var candidateById = existingCandidates.ToDictionary(c => c.Id);

            foreach (var row in resolvedRows)
            {
                if (row.CandidateId.HasValue)
                {
                    var candidate = candidateById[row.CandidateId.Value];

                    // Candidate records are shared, so these detail updates
                    // are reflected wherever this candidate is used.
                    candidate.CandidateName = row.Name;
                    candidate.MobileNumber = row.Mobile;
                    candidate.Email = row.Email;

                    desiredCandidateIds.Add(candidate.Id);
                }
                else
                {
                    var candidate = new Candidate
                    {
                        CandidateName = row.Name,
                        MobileNumber = row.Mobile,
                        Email = row.Email,
                        CreatedOn = DateTime.UtcNow
                    };

                    _context.Candidates.Add(candidate);

                    // Save to obtain the generated Candidate ID.
                    await _context.SaveChangesAsync();

                    existingCandidates.Add(candidate);
                    candidateById[candidate.Id] = candidate;
                    desiredCandidateIds.Add(candidate.Id);
                }
            }

            desiredCandidateIds = desiredCandidateIds.Distinct().ToList();

            // Load enrollment links for THIS batch only.
            var currentEnrollments = await _context.BatchCandidates
                .Where(x => x.BatchRecordId == batch.Id)
                .ToListAsync();

            // Remove candidates absent from the uploaded file from this batch only.
            var enrollmentsToRemove = currentEnrollments
                .Where(x => !desiredCandidateIds.Contains(x.CandidateId))
                .ToList();

            _context.BatchCandidates.RemoveRange(enrollmentsToRemove);

            // Preserve existing enrollment dates and statuses for candidates
            // who remain in the batch.
            var currentlyEnrolledIds = currentEnrollments
                .Where(x => desiredCandidateIds.Contains(x.CandidateId))
                .Select(x => x.CandidateId)
                .ToHashSet();

            foreach (var candidateId in desiredCandidateIds)
            {
                if (currentlyEnrolledIds.Contains(candidateId))
                    continue;

                _context.BatchCandidates.Add(new BatchCandidate
                {
                    BatchRecordId = batch.Id,
                    CandidateId = candidateId,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "Enrolled",
                    CreatedOn = DateTime.UtcNow
                });
            }

            return true;
        }
        private async Task LoadEditDropdownsAsync(EditBatchVM vm)
        {
            vm.Schemes = await _context.Scheme
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SchemeName)
                .Select(s => new SelectListItem
                {
                    Value = s.SchemeId.ToString(),
                    Text = s.SchemeName
                })
                .ToListAsync();

            vm.Projects = await _context.Project
                .AsNoTracking()
                .OrderBy(p => p.ProjectName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.ProjectName
                })
                .ToListAsync();

            vm.TrainingCenters = await _context.TrainingCenter
                .AsNoTracking()
                .OrderBy(c => c.TrainingCenterName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.TrainingCenterName
                })
                .ToListAsync();

            vm.JobRoles = await _context.JobRoles
                .AsNoTracking()
                .Where(j => j.IsActive)
                .OrderBy(j => j.JobRoleTitle)
                .Select(j => new SelectListItem
                {
                    Value = j.Id.ToString(),
                    Text = j.JobRoleTitle
                })
                .ToListAsync();

            vm.Trainers = await _context.TrainerRegistration
                .AsNoTracking()
                .OrderBy(t => t.CandidateName)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.CandidateName
                })
                .ToListAsync();
        }

        [HttpGet]
        public IActionResult DownloadCandidateSample()
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Candidates");

            worksheet.Cell(1, 1).Value = "Candidate Name";
            worksheet.Cell(1, 2).Value = "Mobile Number";
            worksheet.Cell(1, 3).Value = "Email";

            worksheet.Cell(2, 1).Value = "Rahul Sharma";
            worksheet.Cell(2, 2).Value = "9876543210";
            worksheet.Cell(2, 3).Value = "rahul@example.com";

            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Candidate_Sample_Format.xlsx");
        }
    }
}
