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
                        if (string.IsNullOrWhiteSpace(candidate.FatherName))
                        {
                            ModelState.AddModelError(
                                nameof(vm.CandidateFile),
                                $"Father name is missing in Excel row {i + 2}.");
                        }

                        if (string.IsNullOrWhiteSpace(candidate.AadharNo) ||
                            !System.Text.RegularExpressions.Regex.IsMatch(
                                candidate.AadharNo.Trim(), @"^\d{12}$"))
                        {
                            ModelState.AddModelError(
                                nameof(vm.CandidateFile),
                                $"A valid 12-digit Aadhaar number is required in Excel row {i + 2}.");
                        }

                        if (string.IsNullOrWhiteSpace(candidate.DOB))
                        {
                            ModelState.AddModelError(
                                nameof(vm.CandidateFile),
                                $"Date of birth is missing in Excel row {i + 2}.");
                        }
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
                    var duplicateAadhars = importedCandidates
                    .Where(c => !string.IsNullOrWhiteSpace(c.AadharNo))
                    .GroupBy(c => c.AadharNo.Trim())
                    .Any(g => g.Count() > 1);

                    if (duplicateAadhars)
                    {
                        ModelState.AddModelError(
                            nameof(vm.CandidateFile),
                            "The Excel file contains duplicate Aadhaar numbers.");
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
                    var email = imported.Email?.Trim().ToLowerInvariant();
                    var mobile = imported.MobileNumber.Trim();
                    var aadharNo = imported.AadharNo.Trim();

                    // Find candidate using Aadhaar number.
                    var candidate = await _context.Candidates
                        .FirstOrDefaultAsync(c => c.AadharNo == aadharNo);

                    if (candidate == null)
                    {
                        // Aadhaar does not exist: create a new candidate.
                        candidate = new Candidate
                        {
                            CandidateName = imported.CandidateName.Trim(),
                            FatherName = imported.FatherName.Trim(),
                            AadharNo = aadharNo,
                            DOB = imported.DOB?.Trim(),
                            MobileNumber = mobile,
                            Email = email,
                            CreatedOn = now
                        };

                        _context.Candidates.Add(candidate);

                        // Generate the candidate ID before creating enrollment.
                        await _context.SaveChangesAsync();
                    }

                    // Aadhaar already exists: reuse the existing candidate.
                    // Existing personal details and linked Identity account remain unchanged.

                    // Avoid duplicate enrollment in the same batch.
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
                }
                // Save all enrollments.
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                TempData["SuccessMessage"] =
                    $"Batch {batch.BatchId} created successfully. " +
                    $"Candidates processed: {importedCandidates.Count}. ";

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

                    // Approval information
                    ApprovalRemarks = x.ApprovalRemarks,
                    ApprovedOn = x.ApprovedOn,

                    CandidateCount = x.Candidates.Count(),

                    ProjectName = _context.Project
                        .Where(p => p.Id == x.ProjectId)
                        .Select(p => p.ProjectName)
                        .FirstOrDefault(),

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
                        .FirstOrDefault(),

                    // Latest monitoring details for this batch
                    MonitoringDateTime = _context.BatchMonitorings
                        .Where(m => m.BatchId == x.Id)
                        .OrderByDescending(m => m.CreatedOn)
                        .Select(m => (DateTime?)m.MonitoringDateTime)
                        .FirstOrDefault(),

                    MonitoringStatus = _context.BatchMonitorings
                        .Where(m => m.BatchId == x.Id)
                        .OrderByDescending(m => m.CreatedOn)
                        .Select(m => m.Status)
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
            var lastColumn = worksheet.Row(headerRow)
                .LastCellUsed()?.Address.ColumnNumber ?? 0;

            int nameColumn = 0;
            int fatherNameColumn = 0;
            int aadharColumn = 0;
            int dobColumn = 0;
            int mobileColumn = 0;
            int emailColumn = 0;

            for (int column = 1; column <= lastColumn; column++)
            {
                var header = worksheet.Cell(headerRow, column)
                    .GetString()
                    .Trim();

                if (header.Equals("CandidateName", StringComparison.OrdinalIgnoreCase) ||
                    header.Equals("Candidate Name", StringComparison.OrdinalIgnoreCase))
                {
                    nameColumn = column;
                }
                else if (header.Equals("FatherName", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("Father Name", StringComparison.OrdinalIgnoreCase))
                {
                    fatherNameColumn = column;
                }
                else if (header.Equals("AadharNo", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("AadhaarNo", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("Aadhaar Number", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("Aadhar Number", StringComparison.OrdinalIgnoreCase))
                {
                    aadharColumn = column;
                }
                else if (header.Equals("DOB", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("Date of Birth", StringComparison.OrdinalIgnoreCase))
                {
                    dobColumn = column;
                }
                else if (header.Equals("MobileNumber", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("Mobile Number", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("Mobile", StringComparison.OrdinalIgnoreCase))
                {
                    mobileColumn = column;
                }
                else if (header.Equals("Email", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("EmailAddress", StringComparison.OrdinalIgnoreCase) ||
                         header.Equals("Email Address", StringComparison.OrdinalIgnoreCase))
                {
                    emailColumn = column;
                }
            }

            if (nameColumn == 0)
                throw new InvalidDataException(
                    "Excel must contain a CandidateName or Candidate Name column.");

            if (fatherNameColumn == 0)
                throw new InvalidDataException(
                    "Excel must contain a FatherName or Father Name column.");

            if (aadharColumn == 0)
                throw new InvalidDataException(
                    "Excel must contain an Aadhaar Number or AadharNo column.");

            if (dobColumn == 0)
                throw new InvalidDataException(
                    "Excel must contain a DOB or Date of Birth column.");

            if (mobileColumn == 0)
                throw new InvalidDataException(
                    "Excel must contain a MobileNumber, Mobile Number, or Mobile column.");

            if (emailColumn == 0)
                throw new InvalidDataException(
                    "Excel must contain an Email or EmailAddress column.");

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;

            for (int row = headerRow + 1; row <= lastRow; row++)
            {
                var name = worksheet.Cell(row, nameColumn).GetString().Trim();
                var fatherName = worksheet.Cell(row, fatherNameColumn).GetString().Trim();
                var aadharNo = worksheet.Cell(row, aadharColumn).GetString().Trim();
                var dob = worksheet.Cell(row, dobColumn).GetString().Trim();
                var mobile = worksheet.Cell(row, mobileColumn).GetString().Trim();
                var email = worksheet.Cell(row, emailColumn).GetString().Trim();

                // Skip completely empty rows.
                if (string.IsNullOrWhiteSpace(name) &&
                    string.IsNullOrWhiteSpace(fatherName) &&
                    string.IsNullOrWhiteSpace(aadharNo) &&
                    string.IsNullOrWhiteSpace(dob) &&
                    string.IsNullOrWhiteSpace(mobile) &&
                    string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidDataException(
                        $"Candidate name is missing at Excel row {row}.");

                if (string.IsNullOrWhiteSpace(fatherName))
                    throw new InvalidDataException(
                        $"Father name is missing at Excel row {row}.");

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        aadharNo, @"^\d{12}$"))
                {
                    throw new InvalidDataException(
                        $"A valid 12-digit Aadhaar number is required at Excel row {row}.");
                }

                if (string.IsNullOrWhiteSpace(dob))
                    throw new InvalidDataException(
                        $"Date of birth is missing at Excel row {row}.");

                if (string.IsNullOrWhiteSpace(mobile))
                    throw new InvalidDataException(
                        $"Mobile number is missing at Excel row {row}.");

                if (mobile.Length > 20)
                    throw new InvalidDataException(
                        $"Mobile number is too long at Excel row {row}.");

                if (string.IsNullOrWhiteSpace(email) ||
                    !new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                        .IsValid(email))
                {
                    throw new InvalidDataException(
                        $"A valid email address is required at Excel row {row}.");
                }

                candidates.Add(new CandidateExcelRow
                {
                    CandidateName = name,
                    FatherName = fatherName,
                    AadharNo = aadharNo,
                    DOB = dob,
                    MobileNumber = mobile,
                    Email = email
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

            // Normalize uploaded candidate data.
            var candidates = rows.Select(r => new
            {
                Name = r.CandidateName?.Trim() ?? string.Empty,
                FatherName = r.FatherName?.Trim() ?? string.Empty,
                AadharNo = r.AadharNo?.Trim() ?? string.Empty,
                DOB = r.DOB?.Trim() ?? string.Empty,
                Mobile = r.MobileNumber?.Trim() ?? string.Empty,
                Email = string.IsNullOrWhiteSpace(r.Email)
                    ? null
                    : r.Email.Trim().ToLowerInvariant()
            }).ToList();

            // Validate required fields.
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                int excelRow = i + 2;

                if (string.IsNullOrWhiteSpace(c.Name))
                {
                    ModelState.AddModelError(
                        nameof(EditBatchVM.CandidateFile),
                        $"Candidate name is missing in Excel row {excelRow}.");
                }

                if (string.IsNullOrWhiteSpace(c.FatherName))
                {
                    ModelState.AddModelError(
                        nameof(EditBatchVM.CandidateFile),
                        $"Father name is missing in Excel row {excelRow}.");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        c.AadharNo, @"^\d{12}$"))
                {
                    ModelState.AddModelError(
                        nameof(EditBatchVM.CandidateFile),
                        $"A valid 12-digit Aadhaar number is required in Excel row {excelRow}.");
                }

                if (string.IsNullOrWhiteSpace(c.DOB))
                {
                    ModelState.AddModelError(
                        nameof(EditBatchVM.CandidateFile),
                        $"Date of birth is missing in Excel row {excelRow}.");
                }

                if (string.IsNullOrWhiteSpace(c.Mobile))
                {
                    ModelState.AddModelError(
                        nameof(EditBatchVM.CandidateFile),
                        $"Mobile number is missing in Excel row {excelRow}.");
                }

                if (c.Email == null ||
                    !new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                        .IsValid(c.Email))
                {
                    ModelState.AddModelError(
                        nameof(EditBatchVM.CandidateFile),
                        $"A valid email address is required in Excel row {excelRow}.");
                }
            }

            if (!ModelState.IsValid)
                return false;

            // Reject duplicate Aadhaar numbers within this upload only.
            if (candidates
                .GroupBy(c => c.AadharNo, StringComparer.Ordinal)
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError(
                    nameof(EditBatchVM.CandidateFile),
                    "The Excel file contains duplicate Aadhaar numbers.");

                return false;
            }

            var aadhaarNumbers = candidates
                .Select(c => c.AadharNo)
                .ToList();

            // Find existing candidates by Aadhaar number.
            var existingCandidates = await _context.Candidates
                .Where(c => aadhaarNumbers.Contains(c.AadharNo))
                .ToListAsync();

            var candidateByAadhaar = existingCandidates
                .GroupBy(c => c.AadharNo)
                .ToDictionary(g => g.Key, g => g.First());

            // Detect pre-existing duplicate Aadhaar records in the database.
            if (existingCandidates
                .GroupBy(c => c.AadharNo)
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError(
                    nameof(EditBatchVM.CandidateFile),
                    "Duplicate Aadhaar records exist in the candidate database. Please resolve them before updating this batch.");

                return false;
            }

            var desiredCandidateIds = new List<int>();
            var now = DateTime.UtcNow;

            foreach (var row in candidates)
            {
                if (candidateByAadhaar.TryGetValue(
                        row.AadharNo, out var candidate))
                {
                    // Reuse the existing candidate.
                    // Do not overwrite shared personal details.
                    desiredCandidateIds.Add(candidate.Id);
                }
                else
                {
                    // Aadhaar does not exist: create a new candidate.
                    candidate = new Candidate
                    {
                        CandidateName = row.Name,
                        FatherName = row.FatherName,
                        AadharNo = row.AadharNo,
                        DOB = row.DOB,
                        MobileNumber = row.Mobile,
                        Email = row.Email,
                        CreatedOn = now
                    };

                    _context.Candidates.Add(candidate);

                    // Generate the new candidate ID.
                    await _context.SaveChangesAsync();

                    candidateByAadhaar[row.AadharNo] = candidate;
                    desiredCandidateIds.Add(candidate.Id);
                }
            }

            desiredCandidateIds = desiredCandidateIds
                .Distinct()
                .ToList();

            // Load enrollment links for this batch only.
            var currentEnrollments = await _context.BatchCandidates
                .Where(x => x.BatchRecordId == batch.Id)
                .ToListAsync();

            // Remove enrollments for candidates no longer in the upload.
            // Candidate records themselves are never deleted.
            var enrollmentsToRemove = currentEnrollments
                .Where(x => !desiredCandidateIds.Contains(x.CandidateId))
                .ToList();

            _context.BatchCandidates.RemoveRange(enrollmentsToRemove);

            var currentlyEnrolledIds = currentEnrollments
                .Where(x => desiredCandidateIds.Contains(x.CandidateId))
                .Select(x => x.CandidateId)
                .ToHashSet();

            // Add missing enrollments; preserve existing enrollment status/date.
            foreach (var candidateId in desiredCandidateIds)
            {
                if (currentlyEnrolledIds.Contains(candidateId))
                    continue;

                _context.BatchCandidates.Add(new BatchCandidate
                {
                    BatchRecordId = batch.Id,
                    CandidateId = candidateId,
                    EnrollmentDate = now,
                    CreatedOn = now,
                    Status = "Enrolled"
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

            // Column headers
            worksheet.Cell(1, 1).Value = "Candidate Name";
            worksheet.Cell(1, 2).Value = "Father Name";
            worksheet.Cell(1, 3).Value = "Aadhaar Number";
            worksheet.Cell(1, 4).Value = "DOB";
            worksheet.Cell(1, 5).Value = "Mobile Number";
            worksheet.Cell(1, 6).Value = "Email";

            // Sample candidate
            worksheet.Cell(2, 1).Value = "Rahul Sharma";
            worksheet.Cell(2, 2).Value = "Rajesh Sharma";
            worksheet.Cell(2, 3).Value = "123456789012";
            worksheet.Cell(2, 4).Value = "15-08-2000";
            worksheet.Cell(2, 5).Value = "9876543210";
            worksheet.Cell(2, 6).Value = "rahul@example.com";

            // Format Aadhaar and mobile as text to preserve digits.
            worksheet.Column(3).Style.NumberFormat.Format = "@";
            worksheet.Column(5).Style.NumberFormat.Format = "@";

            // Format header
            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Row(1).Style.Fill.BackgroundColor =
                ClosedXML.Excel.XLColor.LightGray;

            worksheet.Columns().AdjustToContents();

            // Freeze header row
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Candidate_Sample_Format.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> Monitoring(int id)
        {
            var tpId = GetCurrentTPId();

            var batch = await _context.Batches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TPId == tpId);

            if (batch == null)
                return NotFound();

            var monitoring = await _context.BatchMonitorings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BatchId == batch.Id);

            var vm = new BatchMonitoringVM
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchId,
                MonitoringDateTime = monitoring?.MonitoringDateTime
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Monitoring(BatchMonitoringVM vm)
        {
            var tpId = GetCurrentTPId();

            var batch = await _context.Batches
                .FirstOrDefaultAsync(x => x.Id == vm.BatchId && x.TPId == tpId);

            if (batch == null)
                return NotFound();

            vm.BatchNumber = batch.BatchId;

            if (vm.MonitoringDateTime.HasValue &&
                vm.MonitoringDateTime.Value <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(vm.MonitoringDateTime),
                    "Monitoring date and time must be in the future.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var monitoring = await _context.BatchMonitorings
                .FirstOrDefaultAsync(x => x.BatchId == batch.Id);

            if (monitoring == null)
            {
                monitoring = new BatchMonitoring
                {
                    BatchId = batch.Id,
                    MonitoringDateTime = vm.MonitoringDateTime!.Value,
                    Status = "Scheduled",
                    CreatedOn = DateTime.UtcNow
                };

                _context.BatchMonitorings.Add(monitoring);
            }
            else
            {
                monitoring.MonitoringDateTime = vm.MonitoringDateTime!.Value;
                monitoring.Status = "Scheduled";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Monitoring scheduled successfully.";

            return RedirectToAction(nameof(Details), new { id = batch.Id });
        }
    }
}
