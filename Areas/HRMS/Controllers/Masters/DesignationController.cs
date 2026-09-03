using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using System.Security.Claims;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers.Masters
{
    [Area("HRMS")]
    [Authorize(Roles = "HR")]
    public class DesignationController : Controller
    {
        private readonly AppdbContext _context;

        public DesignationController(AppdbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var designations = await _context.Designation
                .OrderBy(x => x.DesignationName)
                .ToListAsync();

            return View(designations);
        }


        // =========================================================
        // CREATE / EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int? id)
        {
            // Add
            if (!id.HasValue || id.Value == 0)
            {
                return View(new DesignationVM
                {
                    IsActive = true
                });
            }

            // Edit
            var designation = await _context.Designation
                .FirstOrDefaultAsync(x => x.DesignationId == id.Value);

            if (designation == null)
            {
                return NotFound();
            }

            var model = new DesignationVM
            {
                DesignationId = designation.DesignationId,
                DesignationName = designation.DesignationName,
                IsActive = designation.IsActive
            };

            return View(model);
        }


        // =========================================================
        // CREATE / EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DesignationVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // =====================================================
            // ADD
            // =====================================================

            if (model.DesignationId == 0)
            {
                // Check duplicate designation
                var exists = await _context.Designation
                    .AnyAsync(x =>
                        x.DesignationName.ToLower() ==
                        model.DesignationName.ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        "DesignationName",
                        "Designation already exists.");

                    return View(model);
                }

                var designation = new Designation
                {
                    DesignationName = model.DesignationName.Trim(),
                    IsActive = model.IsActive,
                    CreatedOn = DateTime.Now,
                    CreatedBy = userId
                };

                _context.Designation.Add(designation);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Designation added successfully.";

                return RedirectToAction(nameof(Index));
            }


            // =====================================================
            // EDIT
            // =====================================================

            var existingDesignation = await _context.Designation
                .FirstOrDefaultAsync(x =>
                    x.DesignationId == model.DesignationId);

            if (existingDesignation == null)
            {
                return NotFound();
            }

            // Check duplicate excluding current record
            var duplicate = await _context.Designation
                .AnyAsync(x =>
                    x.DesignationId != model.DesignationId &&
                    x.DesignationName.ToLower() ==
                    model.DesignationName.ToLower());

            if (duplicate)
            {
                ModelState.AddModelError(
                    "DesignationName",
                    "Designation already exists.");

                return View(model);
            }

            existingDesignation.DesignationName =
                model.DesignationName.Trim();

            existingDesignation.IsActive =
                model.IsActive;

            existingDesignation.ModifiedOn =
                DateTime.Now;

            existingDesignation.ModifiedBy =
                userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Designation updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // DEACTIVATE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var designation = await _context.Designation
                .FirstOrDefaultAsync(x =>
                    x.DesignationId == id);

            if (designation == null)
            {
                return NotFound();
            }

            designation.IsActive = false;

            designation.ModifiedOn = DateTime.Now;

            designation.ModifiedBy =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Designation deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // ACTIVATE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var designation = await _context.Designation
                .FirstOrDefaultAsync(x =>
                    x.DesignationId == id);

            if (designation == null)
            {
                return NotFound();
            }

            designation.IsActive = true;

            designation.ModifiedOn = DateTime.Now;

            designation.ModifiedBy =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Designation activated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}