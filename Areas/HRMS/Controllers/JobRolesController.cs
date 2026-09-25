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
        public async Task<IActionResult> Index1()
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
            {
                return RedirectToAction("Login", "Account");
            }
            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                ModelState.AddModelError(
                    "",
                    "Employee record not found.");

                await LoadSubSectors();
                return View(model);
            }

            int employeeId = employee.EmployeeId;
            if (!model.SubSectorId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.SubSectorId),
                    "Please select a Sub Sector.");

                await LoadSubSectors();
                return View(model);
            }

            bool subSectorExists = await _context.SubSectors
                .AnyAsync(x =>
                    x.Id == model.SubSectorId.Value &&
                    x.Status == "Active");

            if (!subSectorExists)
            {
                ModelState.AddModelError(
                    nameof(model.SubSectorId),
                    "Selected Sub Sector is invalid.");

                await LoadSubSectors();
                return View(model);
            }
            bool exists = await _context.JobRoles
                .AnyAsync(x => x.QPCode == model.QPCode);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.QPCode),
                    "QP Code already exists.");

                await LoadSubSectors();
                return View(model);
            }
            if (model.Documents != null &&
                model.Documents.Count > 0)
            {
                foreach (var document in model.Documents)
                {
                    if (document == null)
                        continue;

                    bool noFile =
                        document.File == null ||
                        document.File.Length == 0;
                    if (noFile)
                        continue;
                    if (string.IsNullOrWhiteSpace(document.DocumentType))
                    {
                        ModelState.AddModelError(
                            "Documents",
                            "Please select Document Type for uploaded document.");

                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(document.Language))
                    {
                        ModelState.AddModelError(
                            "Documents",
                            $"Please select Language for {document.DocumentType}.");

                        continue;
                    }
                    string extension =
                        Path.GetExtension(document.File.FileName);

                    if (!string.Equals(
                            extension,
                            ".pdf",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(
                            "Documents",
                            $"{document.File.FileName} must be a PDF file.");

                        continue;
                    }
                    if (!string.Equals(
                            document.File.ContentType,
                            "application/pdf",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(
                            "Documents",
                            $"{document.File.FileName} is not a valid PDF file.");

                        continue;
                    }
                    if (document.File.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(
                            "Documents",
                            $"{document.File.FileName} must not exceed 5 MB.");

                        continue;
                    }
                }
            }
            if (!ModelState.IsValid)
            {
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
                if (model.Documents != null &&
                    model.Documents.Count > 0)
                {
                    foreach (var document in model.Documents)
                    {
                        if (document == null)
                            continue;
                        if (document.File == null ||
                            document.File.Length == 0)
                        {
                            continue;
                        }
                        string filePath = await SaveFile(
                            document.File,
                            jobRole.Id,
                            document.DocumentType,
                            document.Language);
                        var jobRoleDocument = new JobRoleDocument
                        {
                            JobRoleId = jobRole.Id,

                            DocumentType = document.DocumentType,
                            Language = document.Language,

                            FileName = document.File.FileName,
                            FilePath = filePath,

                            CreatedBy = employeeId,
                            CreatedDate = DateTime.Now
                        };

                        _context.JobRoleDocuments.Add(jobRoleDocument);
                    }
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();

                TempData["msg"] =
                    "Job Role added successfully.";

                return RedirectToAction(nameof(Index1));
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
        public async Task<IActionResult> Edit(int id, bool isReopen = false)
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
            bool isPending = string.Equals(
                jobRole.Status,
                "Pending",
                StringComparison.OrdinalIgnoreCase);
            bool isRejectedReopen =
                isReopen &&
                string.Equals(
                    jobRole.Status,
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase);

            if (!isPending && !isRejectedReopen)
            {
                TempData["msg"] =
                    "This Job Role cannot be edited after manager status update.";

                return RedirectToAction(nameof(Index1));
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
                ValidTo = jobRole.ValidTo,
                Documents = jobRole.Documents?
                .Select(d => new JobRoleDocumentViewModel
                {
                    DocumentType = d.DocumentType,
                    Language = d.Language,
                    FileName = d.FileName,
                    FilePath = d.FilePath
                })
                .ToList()
                ?? new List<JobRoleDocumentViewModel>()
            };
            ViewBag.IsReopen = isRejectedReopen;


            await LoadSubSectors();

            return View("Add", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobRoleViewModel model, bool isReopen)
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
            bool isPending = string.Equals(
                jobRole.Status,
                "Pending",
                StringComparison.OrdinalIgnoreCase);
            bool isRejectedReopen =
                isReopen &&
                string.Equals(
                    jobRole.Status,
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase);

            
            if (!isPending && !isRejectedReopen)
            {
                TempData["msg"] =
                    "This Job Role cannot be edited after manager status update.";

                return RedirectToAction(nameof(Index1));
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

                ViewBag.IsReopen = isRejectedReopen;

                return View("Add", model);
            }
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

                ViewBag.IsReopen = isRejectedReopen;

                return View("Add", model);
            }
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
            // ================= DOCUMENT UPDATE =================

            if (model.Documents != null && model.Documents.Any())
            {
                foreach (var document in model.Documents)
                {
                    if (document.File != null && document.File.Length > 0)
                    {
                        var existingDocument = await _context.JobRoleDocuments
                            .FirstOrDefaultAsync(x =>
                                x.JobRoleId == jobRole.Id &&
                                x.DocumentType == document.DocumentType &&
                                x.Language == document.Language);

                        var uploadsFolder = Path.Combine(
                            _environment.WebRootPath,
                            "uploads",
                            "jobroles",
                            jobRole.Id.ToString());

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var fileName = Guid.NewGuid().ToString() +
                                       Path.GetExtension(document.File.FileName);

                        var filePath = Path.Combine(
                            uploadsFolder,
                            fileName);

                        using (var stream = new FileStream(
                            filePath,
                            FileMode.Create))
                        {
                            await document.File.CopyToAsync(stream);
                        }

                        var relativePath =
                            $"/uploads/jobroles/{jobRole.Id}/{fileName}";

                        if (existingDocument != null)
                        {
                            existingDocument.FileName =
                                document.File.FileName;

                            existingDocument.FilePath =
                                relativePath;

                            existingDocument.DocumentType =
                                document.DocumentType;

                            existingDocument.Language =
                                document.Language;
                        }
                        else
                        {
                            var newDocument = new JobRoleDocument
                            {
                                JobRoleId = jobRole.Id,
                                DocumentType = document.DocumentType,
                                Language = document.Language,
                                FileName = document.File.FileName,
                                FilePath = relativePath,
                                CreatedBy = employee.EmployeeId,
                                CreatedDate = DateTime.Now
                            };

                            _context.JobRoleDocuments.Add(newDocument);
                        }
                    }
                }
            }
            if (isRejectedReopen)
            {
                jobRole.Status = "Pending";
                jobRole.ReOpen = true;
            }

            jobRole.ModifiedBy = employee.EmployeeId;
            jobRole.ModifiedDate = DateTime.Now;


            await _context.SaveChangesAsync();
            if (isRejectedReopen)
            {
                TempData["msg"] =
                    "Job Role updated and sent for approval.";
            }
            else
            {
                TempData["msg"] =
                    "Job Role updated successfully.";
            }


            return RedirectToAction(nameof(Index1));
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
            ViewBag.IsEmployee = true;
            ViewBag.ReturnTo = "Approved";

            return View("Approval", jobRoles);
        }
        [HttpGet]
        public async Task<IActionResult> Rejected()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id &&
                    x.IsActive);

            if (employee == null)
                return Unauthorized();

            var jobRoles = await _context.JobRoles
                .Include(x => x.Documents)
                .Where(x =>
                    x.CreatedBy == employee.EmployeeId &&
                    x.Status == "Rejected" &&
                    x.IsActive)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            ViewBag.Status = "Rejected";
            ViewBag.IsEmployee = true;
            ViewBag.ReturnTo = "Rejected";

            return View("Approval", jobRoles);
        }
        [HttpGet]
        public async Task<IActionResult> Approval(string status = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();

            IQueryable<JobRole> query = _context.JobRoles
                .Include(x => x.Documents)
                .Include(x => x.SubSector);
            if (status.Equals("ArchivePending", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.CreatedBy.HasValue &&
                    _context.Employee.Any(e =>
                        e.EmployeeId == x.CreatedBy.Value &&
                        e.ReportingManagerId == employee.EmployeeId
                    ) &&
                    x.Status == "Archive Pending" &&
                    x.IsActive
                );
            }
            else if (status.Equals("Archived", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.CreatedBy.HasValue &&
                    _context.Employee.Any(e =>
                        e.EmployeeId == x.CreatedBy.Value &&
                        e.ReportingManagerId == employee.EmployeeId
                    ) &&
                    x.Status == "Archived" &&
                    !x.IsActive
                );
            }
            else
            {
                query = query.Where(x =>
                    x.CreatedBy.HasValue &&
                    _context.Employee.Any(e =>
                        e.EmployeeId == x.CreatedBy.Value &&
                        e.ReportingManagerId == employee.EmployeeId
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
            ViewBag.ReturnTo = "Approval";

            return View(jobRoles);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string remark)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var employee = await _context.Employee
               .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);
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
            jobRole.ModifiedBy = employee.EmployeeId;
            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Job Role approved successfully.";

            return RedirectToAction(nameof(Approval));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id,string remark)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var employee = await _context.Employee
               .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);
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
            jobRole.ModifiedBy = employee.EmployeeId;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Job Role rejected successfully.";

            return RedirectToAction(nameof(Approval));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveArchive(int id, string? remark)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var manager = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (manager == null)
                return Unauthorized();

            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Status == "Archive Pending" &&
                    x.IsActive);

            if (jobRole == null)
                return NotFound();
            jobRole.Status = "Archived";
            jobRole.IsActive = false;
            jobRole.Remark = remark;
            jobRole.ModifiedBy = manager.EmployeeId;
            jobRole.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Archive request approved successfully.";

            return RedirectToAction(nameof(Approval), new { status = "ArchivePending" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectArchive(int id, string? remark)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var manager = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (manager == null)
                return Unauthorized();

            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Status == "Archive Pending" &&
                    x.IsActive);

            if (jobRole == null)
                return NotFound();

           
            jobRole.Status = "Approved";
            jobRole.IsActive = true;

            jobRole.Remark = remark;
            jobRole.ModifiedBy = manager.EmployeeId;
            jobRole.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Archive request rejected.";

            return RedirectToAction(nameof(Approval), new
            {
                status = "ArchivePending"
            });
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id, string? returnTo = null, string? status = null)
        {
            var jobRole = await _context.JobRoles
                .Include(x => x.Documents)
                .Include(x => x.SubSector)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (jobRole == null)
                return NotFound();

            ViewData["ReturnTo"] = returnTo;
            ViewData["ReturnStatus"] = status;

            return View(jobRole);
        }

        [HttpGet]
        public async Task<IActionResult> Archived()
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
                    x.Status == "Archived" &&
                    !x.IsActive)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            ViewBag.Status = "Archived";
            ViewBag.IsEmployee = true;
            ViewBag.ReturnTo = "Archived";

            return View("Approval", jobRoles);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var employee = await _context.Employee
               .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);
            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x => x.Id == id && x.Status == "Approved");

            if (jobRole == null)
                return NotFound();
            jobRole.Status = "Archive Pending";
            jobRole.IsActive = true;

            jobRole.ModifiedDate = DateTime.Now;
            jobRole.ModifiedBy = employee.EmployeeId;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Job Role archived successfully.";

            return RedirectToAction(nameof(Approved));
        }
        [HttpGet]
        public async Task<IActionResult> ArchivePending()
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
                    x.Status == "Archive Pending" &&
                    x.IsActive)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            ViewBag.Status = "Archive Pending";
            ViewBag.IsEmployee = true;
            ViewBag.ReturnTo = "ArchivePending";

            return View("Approval", jobRoles);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var employee = await _context.Employee
               .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);
            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsActive);

            if (jobRole == null)
                return NotFound();

            jobRole.IsActive = true;
            jobRole.Status = "Approved";
            jobRole.ModifiedDate = DateTime.Now;
            jobRole.ModifiedBy = employee.EmployeeId;
            await _context.SaveChangesAsync();

            TempData["msg"] = "Job Role restored successfully.";

            return RedirectToAction(nameof(Archived), new { status = "Archived" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
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

            var jobRole = await _context.JobRoles
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Status == "Rejected" &&
                    x.IsActive &&
                    x.CreatedBy == employee.EmployeeId);

            if (jobRole == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Edit), new
            {
                id = id,
                isReopen = true
            });
        }
    }
}
