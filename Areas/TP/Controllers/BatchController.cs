using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.TP.Controllers
{
    [Area("TP")]
    [Authorize(Roles = "TrainingPartner")]
    public class BatchController : Controller
    {
        private readonly AppdbContext _context;

        public BatchController(AppdbContext context)
        {
            _context = context;
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
            vm.Projects = ProjectOptions.ToList();

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

            // 4. Validate selected Scheme ID
            var schemeExists = await _context.Scheme.AnyAsync(x =>
                x.SchemeId == vm.SchemeId &&
                x.IsActive);

            if (!schemeExists)
            {
                ModelState.AddModelError(
                    nameof(vm.SchemeId),
                    "Select a valid active scheme.");
            }

            // 5. Validate Project ID against the configured project options
            var projectExists = ProjectOptions.Any(x =>
                x.Value == vm.ProjectId.ToString());

            if (!projectExists)
            {
                ModelState.AddModelError(
                    nameof(vm.ProjectId),
                    "Select a valid project.");
            }

            // 6. Validate Job Role ID
            var jobRoleExists = await _context.JobRoles.AnyAsync(x =>
                x.Id == vm.JobRoleId &&
                x.IsActive);

            if (!jobRoleExists)
            {
                ModelState.AddModelError(
                    nameof(vm.JobRoleId),
                    "Select a valid active job role.");
            }

            // 7. Validate Training Center against all selected IDs
            //var centerExists = await _context.TrainingCenter.AnyAsync(x =>
            //    x.Id == vm.TrainingCenterId &&
            //    x.SchemeId == vm.SchemeId &&
            //    x.ProjectId == vm.ProjectId &&
            //    x.JobRoleId == vm.JobRoleId);

            //if (!centerExists)
            //{
            //    ModelState.AddModelError(
            //        nameof(vm.TrainingCenterId),
            //        "The selected center does not match the scheme, project, and job role.");
            //}

            // 8. Validate Trainer belongs to the current TP
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

            // 9. Read candidates from Excel
            List<BatchCandidate> candidates = new();

            if (validExcelFile && vm.CandidateFile != null)
            {
                try
                {
                    candidates = await ReadCandidatesAsync(vm.CandidateFile);

                    if (candidates.Count == 0)
                    {
                        ModelState.AddModelError(
                            nameof(vm.CandidateFile),
                            "No valid candidates were found in the Excel file.");
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

            // 10. Reload dropdowns if validation fails
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            // 11. Create batch using database IDs
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Forbid();

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

                TPUserId = userId,
                Status = "Draft",
                CreatedOn = DateTime.UtcNow,

                Candidates = candidates
            };

            _context.Batches.Add(batch);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    nameof(vm.BatchId),
                    "The batch could not be saved. Check that the Batch ID is unique and the selected IDs are valid.");

                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            TempData["SuccessMessage"] =
                $"Batch {batch.BatchId} was created with {candidates.Count} candidates.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var batches = await _context.Batches
                .AsNoTracking()
                .Where(x => x.TPUserId == userId)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new
                {
                    x.Id,
                    x.BatchId,
                    x.StartDate,
                    x.EndDate,
                    x.Status,
                    x.CreatedOn,
                    CandidateCount = x.Candidates.Count,
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
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(batches);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var batch = await _context.Batches
                .AsNoTracking()
                .Include(x => x.Candidates)
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.TPUserId == userId);

            if (batch == null)
                return NotFound();

            ViewBag.SchemeName = await _context.Scheme
                .Where(x => x.SchemeId == batch.SchemeId)
                .Select(x => x.SchemeName)
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

            return View(batch);
        }

        private static Task<List<BatchCandidate>> ReadCandidatesAsync(
            IFormFile file)
        {
            var candidates = new List<BatchCandidate>();

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

                candidates.Add(new BatchCandidate
                {
                    CandidateName = name,
                    MobileNumber = mobile,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    CreatedOn = DateTime.UtcNow
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
    }
}
