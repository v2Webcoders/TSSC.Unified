using DocumentFormat.OpenXml.InkML;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Areas.HRMS.Controllers;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProjectManagementController : Controller
    {
        private readonly AppdbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;

        public ProjectManagementController(AppdbContext db, IWebHostEnvironment environment,UserManager<AppUser> userManager)
        {
            _db=db;
            _environment = environment;
            _userManager = userManager;

        }
        private async Task LoadDropdowns(AdminProjectViewModel model)
        {
            // Project Type - Only 3 Parent Categories
            model.ProjectTypeList = await _db.ProjectType
                .AsNoTracking()
                .Where(x => x.IsActive && x.ParentProjectTypeId == null)
                .Select(x => new SelectListItem
                {
                    Value = x.ProjectTypeId.ToString(),
                    Text = x.ProjectTypeName
                })
                .ToListAsync();


            // Business Head
            model.BusinessHeadList = await (
                from e in _db.Employee.AsNoTracking()
                join ur in _db.UserRoles
                    on e.ApplicationUserId equals ur.UserId
                join r in _db.Roles
                    on ur.RoleId equals r.Id
                where r.Name == "BusinessHead"
                select new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = e.FirstName + " " + e.LastName
                }
            ).Distinct().ToListAsync();


            // Project Manager
            model.ProjectManagerList = await _db.Employee
                .AsNoTracking()
                .Select(x => new SelectListItem
                {
                    Value = x.EmployeeId.ToString(),
                    Text = x.FirstName + " " + x.LastName
                })
                .ToListAsync();


            // Project Location - State Dropdown
            model.ProjectLocationList = await _db.State
                .AsNoTracking()
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.StateName
                })
                .ToListAsync();


            // Government ID - Only fetch ProjectTypeId
            var governmentId = await _db.ProjectType
                .AsNoTracking()
                .Where(x => x.IsActive &&
                            x.ProjectTypeName == "Government" &&
                            x.ParentProjectTypeId == null)
                .Select(x => (int?)x.ProjectTypeId)
                .FirstOrDefaultAsync();


            // Government Submenu - State and Central
            model.ProjectSubTypeList = new List<SelectListItem>();

            if (governmentId.HasValue)
            {
                model.ProjectSubTypeList = await _db.ProjectType
                    .AsNoTracking()
                    .Where(x => x.IsActive &&
                                x.ParentProjectTypeId == governmentId.Value)
                    .Select(x => new SelectListItem
                    {
                        Value = x.ProjectTypeId.ToString(),
                        Text = x.ProjectTypeName
                    })
                    .ToListAsync();
            }
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var projects = await _db.AdminProjects
                .AsNoTracking()
                .OrderByDescending(x => x.ProjectId)
                .ToListAsync();

            return View(projects);
        }
        [HttpGet]
        public async Task<IActionResult> AddProject()
        {
            var model = new AdminProjectViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };

            await LoadDropdowns(model);

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var project = await _db.AdminProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            // Project Type
            ViewBag.ProjectTypeName = await _db.ProjectType
                .Where(x => x.ProjectTypeId == project.ProjectTypeId)
                .Select(x => x.ProjectTypeName)
                .FirstOrDefaultAsync();

            // Project Sub Type (State / Central)
            ViewBag.ProjectSubTypeName = project.ProjectSubTypeId.HasValue
                ? await _db.ProjectType
                    .Where(x => x.ProjectTypeId == project.ProjectSubTypeId.Value)
                    .Select(x => x.ProjectTypeName)
                    .FirstOrDefaultAsync()
                : null;

            // Project Location
            ViewBag.ProjectLocationName = project.ProjectLocationId.HasValue
                ? await _db.State
                    .Where(x => x.Id == project.ProjectLocationId.Value)
                    .Select(x => x.StateName)
                    .FirstOrDefaultAsync()
                : null;

            // Project Manager
            ViewBag.ProjectManagerName = await _db.Employee
                .Where(x => x.EmployeeId == project.ProjectManagerId)
                .Select(x => x.FirstName + " " + x.LastName)
                .FirstOrDefaultAsync();

            // Business Head
            ViewBag.BusinessHeadName = await _db.Employee
     .Where(x => x.EmployeeId == project.BusinessHeadId)
     .Select(x => x.FirstName + " " + x.LastName)
     .FirstOrDefaultAsync();

            return View(project);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProject(AdminProjectViewModel model)
        {
            // Date Validation
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "End date cannot be earlier than start date.");
            }

            // Attachment Validation
            if (model.AttachmentFile != null)
            {
                string extension = Path.GetExtension(
                    model.AttachmentFile.FileName).ToLowerInvariant();

                string[] allowedExtensions =
                {
            ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
        };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "AttachmentFile",
                        "Only PDF, DOC, DOCX, JPG, JPEG and PNG files are allowed.");
                }

                if (model.AttachmentFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "AttachmentFile",
                        "File size cannot exceed 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model);
                return View(model);
            }

            string attachmentPath = null;

            try
            {
                // Save Attachment
                if (model.AttachmentFile != null &&
                    model.AttachmentFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "projects");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string extension = Path.GetExtension(
                        model.AttachmentFile.FileName);

                    string fileName = Guid.NewGuid().ToString() + extension;

                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(
                        filePath, FileMode.Create))
                    {
                        await model.AttachmentFile.CopyToAsync(stream);
                    }

                    attachmentPath = "/uploads/projects/" + fileName;
                }

                // Create Project
                var project = new AdminProject
                {
                    ProjectTypeId = model.ProjectTypeId,
                    ProjectSubTypeId = model.ProjectSubTypeId,
                    ProjectName = model.ProjectName,
                    Department = model.Department,
                    ProjectDescription = model.ProjectDescription,
                   
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,

                    ProjectCost = model.ProjectCost,
                    ProjectLocationId = model.ProjectLocationId,

                    ProjectManagerId = model.ProjectManagerId.Value,
                    BusinessHeadId = model.BusinessHeadId,

                    Attachment = attachmentPath,

                    CreatedBy = User.Identity?.Name,
                    CreatedDate = DateTime.Now,

                    IsActive = true
                };

                _db.AdminProjects.Add(project);

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project added successfully.";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // Delete uploaded file if database save fails
                if (!string.IsNullOrEmpty(attachmentPath))
                {
                    string savedFile = Path.Combine(
                        _environment.WebRootPath,
                        attachmentPath.TrimStart('/')
                            .Replace('/', Path.DirectorySeparatorChar));

                    if (System.IO.File.Exists(savedFile))
                    {
                        System.IO.File.Delete(savedFile);
                    }
                }

                ModelState.AddModelError(
                    "",
                    "Something went wrong while saving the project.");

                await LoadDropdowns(model);

                return View(model);
            }
        }

    }
}
