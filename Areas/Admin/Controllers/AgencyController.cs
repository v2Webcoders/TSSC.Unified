using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;

namespace TSSC.Unified.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AgencyController :Controller
    {
        private readonly AppdbContext _db;

        public AgencyController(AppdbContext db)
        {
            _db=db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var agencies = await _db.AssessmentAgency
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(agencies);
        }

        // =========================
        // ADD AGENCY - GET
        // =========================
        [HttpGet]
        public IActionResult AddAgency()
        {
            return View();
        }

        // =========================
        // ADD AGENCY - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAgency(AssessmentAgency model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
       .Where(x => x.Value != null && x.Value.Errors.Count > 0)
       .SelectMany(x => x.Value!.Errors.Select(e =>
           $"{x.Key}: {e.ErrorMessage}"))
       .ToList();

                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }
                return View(model);
            }

            var agencyName = model.AgencyName?.Trim();

            if (string.IsNullOrWhiteSpace(agencyName))
            {
                ModelState.AddModelError(
                    "AgencyName",
                    "Agency Name is required.");

                return View(model);
            }

            // Duplicate agency check
            var alreadyExists = await _db.AssessmentAgency
                .AnyAsync(x =>
                    x.AgencyName.ToLower() == agencyName.ToLower());

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    "AgencyName",
                    "This Assessment Agency already exists.");

                return View(model);
            }

            var agency = new AssessmentAgency
            {
                AgencyName = agencyName,
                IsActive = true,
                CreatedDate = DateTime.Now
                // CreatedBy = ...
            };

            _db.AssessmentAgency.Add(agency);

            await _db.SaveChangesAsync();

            TempData["msg"] =
                "Assessment Agency added successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}

