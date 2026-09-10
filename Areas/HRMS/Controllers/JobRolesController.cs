using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Areas.Admin.Controllers;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{

    [Area("HRMS")]
    [Authorize]
    public class JobRolesController : Controller
    {
        private readonly ILogger<CertificateController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        public JobRolesController(
            ILogger<CertificateController> logger,
            AppdbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return Unauthorized();
            }

            var jobRoles = await _context.JobRoles
                .Include(x => x.Documents)
                .Where(x => x.CreatedBy == employee.EmployeeId && x.IsActive)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(jobRoles);
        }
        private async Task LoadSubSectors()
        {
            ViewBag.SubSectors = await _context.SubSectors
                .Where(x => x.Status == "Active")
                .OrderBy(x => x.SubSectorName)
                .ToListAsync();
        }
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            await LoadSubSectors();

            return View(new JobRoleViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(JobRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSubSectors();
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                ModelState.AddModelError("", "Employee record not found.");

                await LoadSubSectors();
                return View(model);
            }

            int employeeId = employee.EmployeeId;

            var exists = await _context.JobRoles
                .AnyAsync(x => x.QPCode == model.QPCode);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.QPCode),
                    "QP Code already exists.");

                await LoadSubSectors();
                return View(model);
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var jobRole = new JobRole
                {
                    QPCode = model.QPCode,
                    JobRoleTitle = model.JobRoleTitle,
                    Description = model.Description,
                    SubSectorId = model.SubSectorId,
                    QPVersion = model.QPVersion,
                    NSQFLevel = model.NSQFLevel,
                    EducationQualification = model.EducationQualification,
                    QPHours = model.QPHours,
                    ValidFrom = model.ValidFrom,
                    ValidTo = model.ValidTo,

                    Status = "Pending",
                    IsActive = true,

                    CreatedBy = employeeId,
                    CreatedDate = DateTime.Now
                };

                _context.JobRoles.Add(jobRole);

                await _context.SaveChangesAsync();

                if (model.Documents != null)
                {
                    foreach (var document in model.Documents)
                    {
                        if (document == null ||
                            string.IsNullOrWhiteSpace(document.DocumentType) ||
                            string.IsNullOrWhiteSpace(document.Language) ||
                            document.File == null ||
                            document.File.Length == 0)
                        {
                            continue;
                        }

                        // Allow PDF only
                        var extension = Path.GetExtension(document.File.FileName);

                        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError(
                                "Documents",
                                $"Only PDF files are allowed for {document.DocumentType}."
                            );

                            continue;
                        }

                        // Validate MIME type
                        if (!string.Equals(
                                document.File.ContentType,
                                "application/pdf",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError(
                                "Documents",
                                $"Only PDF files are allowed for {document.DocumentType}."
                            );

                            continue;
                        }

                        // Optional: Maximum file size = 5 MB
                        if (document.File.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError(
                                "Documents",
                                $"{document.DocumentType} PDF must not exceed 5 MB."
                            );

                            continue;
                        }

                        var filePath = await SaveFile(
                            document.File,
                            jobRole.Id,
                            document.DocumentType,
                            document.Language);

                        _context.JobRoleDocuments.Add(
                            new JobRoleDocument
                            {
                                JobRoleId = jobRole.Id,
                                DocumentType = document.DocumentType,
                                Language = document.Language,
                                FileName = document.File.FileName,
                                FilePath = filePath,
                                CreatedBy = employeeId,
                                CreatedDate = DateTime.Now
                            });
                    }

                    // Important: don't save if validation failed
                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                TempData["msg"] =
                    "Job Role added successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Error while adding Job Role.");

                ModelState.AddModelError(
                    "",
                    "Unable to save Job Role. Please try again.");

                await LoadSubSectors();

                return View(model);
            }
        }

        private async Task<string> SaveFile( IFormFile file,int jobRoleId,string documentType,string language)
        {
            var folder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "jobroles",
                jobRoleId.ToString());

            Directory.CreateDirectory(folder);


            var extension = Path.GetExtension(file.FileName);

            var fileName =
                $"{documentType}_{language}_{Guid.NewGuid():N}{extension}";

            var fullPath = Path.Combine(folder, fileName);


            await using var stream =
                new FileStream(fullPath, FileMode.Create);

            await file.CopyToAsync(stream);


            return $"/uploads/jobroles/{jobRoleId}/{fileName}";
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();

            var jobRole = await _context.JobRoles
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.CreatedBy == employee.EmployeeId &&
                    x.IsActive);

            if (jobRole == null)
                return NotFound();

            // Only Pending Job Roles can be edited
            if (!string.Equals(
                    jobRole.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["msg"] =
                    "This Job Role cannot be edited after manager status update.";

                return RedirectToAction(nameof(Index));
            }

            var model = new JobRoleViewModel
            {
                Id = jobRole.Id,
                QPCode = jobRole.QPCode,
                JobRoleTitle = jobRole.JobRoleTitle,
                Description = jobRole.Description,
                SubSectorId = jobRole.SubSectorId,
                QPVersion = jobRole.QPVersion,
                NSQFLevel = jobRole.NSQFLevel,
                EducationQualification = jobRole.EducationQualification,
                QPHours = jobRole.QPHours,
                ValidFrom = jobRole.ValidFrom,
                ValidTo = jobRole.ValidTo
            };

            // Load Sub Sectors
            await LoadSubSectors();

            // Use Add.cshtml for Edit
            return View("Add", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobRoleViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();

            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    x.CreatedBy == employee.EmployeeId &&
                    x.IsActive);

            if (jobRole == null)
                return NotFound();

            // Only Pending Job Roles can be edited
            if (!string.Equals(
                    jobRole.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["msg"] =
                    "This Job Role cannot be edited after manager status update.";

                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Any())
                    .SelectMany(x => x.Value.Errors.Select(e =>
                        $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }

                await LoadSubSectors();

                return View("Add", model);
            }

            // Check duplicate QP Code
            // Exclude the current Job Role
            var exists = await _context.JobRoles
                .AnyAsync(x =>
                    x.QPCode == model.QPCode &&
                    x.Id != model.Id);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.QPCode),
                    "QP Code already exists.");

                await LoadSubSectors();

                return View("Add", model);
            }

            // Update fields
            jobRole.QPCode = model.QPCode;
            jobRole.JobRoleTitle = model.JobRoleTitle;
            jobRole.Description = model.Description;
            jobRole.SubSectorId = model.SubSectorId;
            jobRole.QPVersion = model.QPVersion;
            jobRole.NSQFLevel = model.NSQFLevel;
            jobRole.EducationQualification = model.EducationQualification;
            jobRole.QPHours = model.QPHours;
            jobRole.ValidFrom = model.ValidFrom;
            jobRole.ValidTo = model.ValidTo;

            jobRole.ModifiedBy = employee.EmployeeId;
            jobRole.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Job Role updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Approved()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();

            var jobRoles = await _context.JobRoles
                .Include(x => x.Documents)
                .Include(x => x.SubSector)
                .Where(x =>
                    x.CreatedBy == employee.EmployeeId &&
                    x.Status == "Approved" &&
                    x.IsActive)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            ViewBag.Status = "Approved";
            ViewBag.IsEmployee = false;

            return View("Approval", jobRoles);
        }
        [HttpGet]
        public async Task<IActionResult> Approval(string status = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            // Logged-in manager
            var manager = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (manager == null)
                return Unauthorized();

            IQueryable<JobRole> query = _context.JobRoles
                .Include(x => x.Documents)
                .Include(x => x.SubSector);

            if (status.Equals("Archived", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.CreatedBy.HasValue &&
                    !_context.Employee.Any(e =>
                        e.EmployeeId == x.CreatedBy.Value &&
                        e.ReportingManagerId == manager.EmployeeId &&
                        e.IsActive
                    ) == false &&
                    !x.IsActive
                );
            }
            else
            {
                query = query.Where(x =>
                    x.CreatedBy.HasValue &&
                    _context.Employee.Any(e =>
                        e.EmployeeId == x.CreatedBy.Value &&
                        e.ReportingManagerId == manager.EmployeeId &&
                        e.IsActive
                    ) &&
                    x.Status == status &&
                    x.IsActive
                );
            }

            var jobRoles = await query
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.IsEmployee = false;

            return View(jobRoles);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string remark)
        {
            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (jobRole == null)
                return NotFound();

            // Only Pending Job Roles can be approved
            if (!string.Equals(
                    jobRole.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] =
                    "This Job Role has already been processed.";

                return RedirectToAction(nameof(Approval));
            }

            jobRole.Status = "Approved";
            jobRole.Remark = remark;
            jobRole.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Job Role approved successfully.";

            return RedirectToAction(nameof(Approval));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id,string remark)
        {
            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (jobRole == null)
                return NotFound();

            // Only Pending Job Roles can be rejected
            if (!string.Equals(
                    jobRole.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] =
                    "This Job Role has already been processed.";

                return RedirectToAction(nameof(Approval));
            }

            jobRole.Status = "Rejected";
            jobRole.Remark = remark;
            jobRole.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Job Role rejected successfully.";

            return RedirectToAction(nameof(Approval));
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var jobRole = await _context.JobRoles
                .Include(x => x.Documents)
                .Include(x => x.SubSector)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (jobRole == null)
                return NotFound();

            return View(jobRole);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x => x.Id == id && x.Status == "Approved");

            if (jobRole == null)
                return NotFound();

            jobRole.IsActive = false;
            jobRole.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Job Role archived successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
