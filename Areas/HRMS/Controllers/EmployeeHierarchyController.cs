using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Areas.HRMS.Controllers;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class EmployeeHierarchyController :Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public EmployeeHierarchyController(ILogger<EmployeeController> logger,
            AppdbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager, IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }
        [HttpGet]
        public async Task<IActionResult> EmployeeHierarchy()
        {
            var hierarchy = await _context.EmployeeHierarchy
                .FirstOrDefaultAsync(x => x.IsActive);

            if (hierarchy != null)
            {
                var model = new EmployeeHierarchyVM
                {
                    HierarchyId = hierarchy.HierarchyId,
                    HierarchyDocument = hierarchy.HierarchyDocument,
                    CreatedOn = hierarchy.CreatedOn,
                    UpdatedOn = hierarchy.UpdatedOn,
                    IsActive = hierarchy.IsActive
                };

                return View(model);
            }

            return View(new EmployeeHierarchyVM());
        }
        [HttpPost]
        public async Task<IActionResult> EmployeeHierarchy(EmployeeHierarchyVM model)
        {
            if (model.HierarchyFile == null && model.HierarchyId == 0)
            {
                ModelState.AddModelError(
                    "HierarchyFile",
                    "Please select a PDF file."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var hierarchy = await _context.EmployeeHierarchy
                .FirstOrDefaultAsync(x => x.IsActive);

            // Upload new PDF
            if (model.HierarchyFile != null &&
                model.HierarchyFile.Length > 0)
            {
                var extension = Path.GetExtension(
                    model.HierarchyFile.FileName
                ).ToLower();

                if (extension != ".pdf")
                {
                    ModelState.AddModelError(
                        "HierarchyFile",
                        "Only PDF files are allowed."
                    );

                    return View(model);
                }

                var folderPath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "employeehierarchy"
                );

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var fileName =
                    Guid.NewGuid().ToString() + extension;

                var filePath = Path.Combine(
                    folderPath,
                    fileName
                );

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await model.HierarchyFile.CopyToAsync(stream);
                }

                var documentPath =
                    "/uploads/employeehierarchy/" + fileName;

                // First time upload
                if (hierarchy == null)
                {
                    hierarchy = new EmployeeHierarchy
                    {
                        HierarchyDocument = documentPath,
                        CreatedOn = DateTime.Now,
                        IsActive = true
                    };

                    _context.EmployeeHierarchy.Add(hierarchy);
                }
                else
                {
                    // Replace existing PDF
                    hierarchy.HierarchyDocument = documentPath;
                    hierarchy.UpdatedOn = DateTime.Now;

                    _context.EmployeeHierarchy.Update(hierarchy);
                }
            }

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Organization Chart uploaded successfully.";

            return RedirectToAction("EmployeeHierarchy");
        }
        //[HttpGet]
        //public async Task<IActionResult> HierarchyView()
        //{
        //    var hierarchy = await _context.EmployeeHierarchy
        //        .FirstOrDefaultAsync(x => x.IsActive);

        //    if (hierarchy == null || string.IsNullOrEmpty(hierarchy.HierarchyDocument))
        //    {
        //        TempData["msg"] = "Employee Hierarchy PDF not found.";
        //        return RedirectToAction("HierarchyView");
        //    }

        //    return Redirect(hierarchy.HierarchyDocument);
        //}
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> HierarchyView()
        {
            var hierarchy =
                await _context.EmployeeHierarchy
                    .Where(x => x.IsActive)
                    .FirstOrDefaultAsync();

            if (hierarchy == null ||
                string.IsNullOrWhiteSpace(hierarchy.HierarchyDocument))
            {
                

                return NotFound("Organization Chart PDF not found.");
            }

            return Redirect(hierarchy.HierarchyDocument);
        }
    }
}
