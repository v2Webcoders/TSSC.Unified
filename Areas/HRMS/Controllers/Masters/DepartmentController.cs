using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
using System.Security.Claims;
using QUIZAPP;

namespace TSSC.Unified.Areas.HRMS.Controllers.Masters
{
    [Area("HRMS")]
    [Authorize(Roles = "HR")]
    public class DepartmentController : Controller
    {
        private readonly AppdbContext _context;

        public DepartmentController(AppdbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Department
                .OrderBy(x => x.DepartmentName)
                .ToListAsync();

            return View(departments);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        // =====================================================
        // CREATE / EDIT - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create(int? id)
        {
            // Add
            if (!id.HasValue || id.Value == 0)
            {
                return View(new DepartmentVM
                {
                    IsActive = true
                });
            }

            // Edit
            var department = await _context.Department
                .FirstOrDefaultAsync(x => x.DepartmentId == id.Value);

            if (department == null)
                return NotFound();

            var model = new DepartmentVM
            {
                DepartmentId = department.DepartmentId,
                DepartmentName = department.DepartmentName,
                IsActive = department.IsActive
            };

            return View(model);
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        // =====================================================
        // CREATE / EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // =====================================================
            // CREATE
            // =====================================================
            if (model.DepartmentId == 0)
            {
                var exists = await _context.Department
                    .AnyAsync(x =>
                        x.DepartmentName.ToLower() ==
                        model.DepartmentName.Trim().ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        "DepartmentName",
                        "Department already exists.");

                    return View(model);
                }

                var department = new Department
                {
                    DepartmentName = model.DepartmentName.Trim(),
                    IsActive = model.IsActive,
                    CreatedOn = DateTime.Now,
                    CreatedBy = userId
                };

                _context.Department.Add(department);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Department added successfully.";
            }
            // =====================================================
            // UPDATE
            // =====================================================
            else
            {
                var department = await _context.Department
                    .FirstOrDefaultAsync(x =>
                        x.DepartmentId == model.DepartmentId);

                if (department == null)
                    return NotFound();

                var exists = await _context.Department
                    .AnyAsync(x =>
                        x.DepartmentId != model.DepartmentId &&
                        x.DepartmentName.ToLower() ==
                        model.DepartmentName.Trim().ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        "DepartmentName",
                        "Department already exists.");

                    return View(model);
                }

                department.DepartmentName =
                    model.DepartmentName.Trim();

                department.IsActive = model.IsActive;
                department.ModifiedOn = DateTime.Now;
                department.ModifiedBy = userId;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Department updated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        

        // =====================================================
        // DELETE / DEACTIVATE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Department
                .FirstOrDefaultAsync(x => x.DepartmentId == id);

            if (department == null)
                return NotFound();

            // Soft delete
            department.IsActive = false;
            department.ModifiedOn = DateTime.Now;
            department.ModifiedBy =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Department deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ACTIVATE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var department = await _context.Department
                .FirstOrDefaultAsync(x => x.DepartmentId == id);

            if (department == null)
                return NotFound();

            department.IsActive = true;
            department.ModifiedOn = DateTime.Now;
            department.ModifiedBy =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Department activated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
