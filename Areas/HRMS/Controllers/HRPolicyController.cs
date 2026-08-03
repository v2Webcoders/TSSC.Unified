using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP.ViewModel;
using System;
using System.Diagnostics;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QUIZAPP.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "HR")]
    public class HRPolicyController : Controller
    {
        

        private readonly ILogger<EmployeeController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        public HRPolicyController(
            ILogger<EmployeeController> logger,
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
        public async Task<IActionResult> Create(int? id)
        {
            HRPolicyVM model = new HRPolicyVM();

            if (id.HasValue)
            {
                var policy = await _context.HRPolicies.FindAsync(id.Value);

                if (policy == null)
                    return NotFound();

                model = new HRPolicyVM
                {
                    PolicyId = policy.PolicyId,
                    PolicyTitle = policy.PolicyTitle,
                    Description = policy.Description,
                    PolicyDocument = policy.PolicyDocument,
                    IsActive = policy.IsActive,
                    CreatedBy = policy.CreatedBy,
                    CreatedOn = policy.CreatedOn,
                    ModifiedBy = policy.ModifiedBy,
                    ModifiedOn = policy.ModifiedOn
                };
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HRPolicyVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string fileName = model.PolicyDocument;

            // Upload PDF
            if (model.PolicyFile != null)
            {
                // Delete old file while editing
                if (!string.IsNullOrEmpty(model.PolicyDocument))
                {
                    string oldFile = Path.Combine(_environment.WebRootPath,
                                                  "uploads/policies",
                                                  model.PolicyDocument);

                    if (System.IO.File.Exists(oldFile))
                        System.IO.File.Delete(oldFile);
                }

                fileName = Guid.NewGuid() + Path.GetExtension(model.PolicyFile.FileName);

                string folder = Path.Combine(_environment.WebRootPath, "uploads", "policies");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await model.PolicyFile.CopyToAsync(stream);
                }
            }

            // Make only one policy active
            if (model.IsActive)
            {
                var activePolicies = await _context.HRPolicies
                    .Where(x => x.IsActive && x.PolicyId != model.PolicyId)
                    .ToListAsync();

                foreach (var item in activePolicies)
                    item.IsActive = false;
            }

            if (model.PolicyId == 0)
            {
                var policy = new HRPolicy
                {
                    PolicyTitle = model.PolicyTitle,
                    Description = model.Description,
                    PolicyDocument = fileName,
                    IsActive = true,                     // Always active for new policy
                    CreatedBy = User.Identity!.Name,
                    CreatedOn = DateTime.Now
                };

                _context.HRPolicies.Add(policy);

                TempData["SuccessMessage"] = "HR Policy added successfully.";
            }
            else
            {
                var policy = await _context.HRPolicies.FindAsync(model.PolicyId);

                if (policy == null)
                    return NotFound();

                policy.PolicyTitle = model.PolicyTitle;
                policy.Description = model.Description;

                if (!string.IsNullOrEmpty(fileName))
                    policy.PolicyDocument = fileName;

                policy.IsActive = model.IsActive;
                policy.ModifiedBy = User.Identity!.Name;
                policy.ModifiedOn = DateTime.Now;

                TempData["SuccessMessage"] = "HR Policy updated successfully.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Index()
        {
            var model = await _context.HRPolicies
                                      .OrderByDescending(x => x.IsActive)
                                      .ThenByDescending(x => x.CreatedOn)
                                      .ToListAsync();

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var policy = await _context.HRPolicies.FindAsync(id);

            if (policy == null)
            {
                TempData["ErrorMessage"] = "HR Policy not found.";
                return RedirectToAction(nameof(Index));
            }

            // Delete PDF file
            if (!string.IsNullOrEmpty(policy.PolicyDocument))
            {
                string filePath = Path.Combine(_environment.WebRootPath,
                                               "uploads",
                                               "policies",
                                               policy.PolicyDocument);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.HRPolicies.Remove(policy);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "HR Policy deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }

}
    

