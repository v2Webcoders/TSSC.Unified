using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "Finance")]
    public class KPIMasterController : Controller
    {
        private static readonly string[] KpiNames =
            { "Gross Revenue", "Surplus", "Govt Business", "Non Govt Business" };
        private const string Unit = "₹ Cr";

        private readonly AppdbContext _context;   // <- use your DbContext name
        private readonly ILogger<KPIMasterController> _logger;

        public KPIMasterController(AppdbContext context, ILogger<KPIMasterController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Same rule as the menu: Finance role + Manager designation.
        private async Task<bool> CanAddKpiAsync()
        {
            var employee = await _context.Employee                       // <- adapt to your entity
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.OfficialEmail == User.Identity!.Name); // <- adapt the lookup
            return employee?.EmployeeCode?.Trim()
                .Equals("C015/82", StringComparison.OrdinalIgnoreCase) == true;
        }

        // Four rows, pre-filled with the most recently saved values so only changes need typing.
        private async Task<KpiCreateViewModel> BuildModelAsync()
        {
            var model = new KpiCreateViewModel();
            foreach (var name in KpiNames)
            {
                var last = await _context.KpiMasters
                    .Where(k => k.KpiName == name)
                    .OrderByDescending(k => k.CreatedOn).ThenByDescending(k => k.Id)
                    .FirstOrDefaultAsync();

                model.Items.Add(new KpiItemViewModel
                {
                    KpiName = name,
                    TotalValue = last?.TotalValue,
                    FromDate = last?.FromDate,
                    ToDate = last?.ToDate,
                    ReceivedValue = last?.ReceivedValue
                });
            }
            return model;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await CanAddKpiAsync()) return Forbid();
            return View(await BuildModelAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KpiCreateViewModel model)
        {
            if (!await CanAddKpiAsync()) return Forbid();

            if (model.Items.Count != KpiNames.Length)
            {
                ModelState.AddModelError("", $"Exactly {KpiNames.Length} KPIs are required.");
                return View(await BuildModelAsync());
            }
            // Names always come from the server, never from the posted form
            for (int i = 0; i < KpiNames.Length; i++) model.Items[i].KpiName = KpiNames[i];

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.FromDate.HasValue && item.ToDate.HasValue && item.ToDate < item.FromDate)
                    ModelState.AddModelError($"Items[{i}].ToDate", "To date cannot be earlier than from date.");
                if (item.TotalValue.HasValue && item.ReceivedValue.HasValue && item.ReceivedValue > item.TotalValue)
                    ModelState.AddModelError($"Items[{i}].ReceivedValue", "Received cannot be more than total.");
            }

            if (!ModelState.IsValid)
            {
                // Log exactly what failed so it is easy to see in the output/log window
                foreach (var e in ModelState.Where(x => x.Value!.Errors.Count > 0))
                    _logger.LogWarning("KPI save validation: {Field} -> {Errors}", e.Key,
                        string.Join("; ", e.Value!.Errors.Select(x => x.ErrorMessage)));
                return View(model);
            }

            var now = DateTime.Now;
            var user = User.Identity!.Name ?? "";

            _context.KpiMasters.AddRange(model.Items.Select(i => new KpiMaster
            {
                KpiName = i.KpiName,
                Unit = Unit,
                FromDate = i.FromDate!.Value.Date,
                ToDate = i.ToDate!.Value.Date,
                TotalValue = i.TotalValue!.Value,
                ReceivedValue = i.ReceivedValue!.Value,
                CreatedBy = user,
                CreatedOn = now
            }));
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KPI save failed");
                ModelState.AddModelError("", "Could not save: " + ex.GetBaseException().Message);
                return View(model);
            }

            TempData["Success"] = "KPIs saved. The CEO Dashboard now shows the updated values.";
            return RedirectToAction(nameof(Create));
        }
    }

}
