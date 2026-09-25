using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;

[Area("HRMS")]
[Authorize]
public class CalendarController : Controller
{
    private readonly AppdbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public CalendarController(
        AppdbContext context,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    // =====================================================
    // HOLIDAY CALENDAR
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Holidays(
        int? year,
        int? month)
    {
        var currentUser =
            await _userManager.GetUserAsync(User);

        if (currentUser == null)
            return Unauthorized();

        //bool isHR =
        //    User.IsInRole("HR");

        //if (!isHR)
        //    return Forbid();


        var selectedYear =
            year ?? DateTime.Today.Year;

        var selectedMonth =
            month ?? DateTime.Today.Month;


        if (selectedMonth < 1)
            selectedMonth = 1;

        if (selectedMonth > 12)
            selectedMonth = 12;


        var firstDate =
            new DateTime(
                selectedYear,
                selectedMonth,
                1);

        var lastDate =
            firstDate.AddMonths(1).AddDays(-1);


        var holidays =
            await _context.Holiday
                .Where(x =>
                    x.HolidayDate >= firstDate &&
                    x.HolidayDate <= lastDate &&
                    x.Status == "Published")
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();


        ViewBag.Year =
            selectedYear;

        ViewBag.Month =
            selectedMonth;

        ViewBag.FirstDate =
            firstDate;

        ViewBag.LastDate =
            lastDate;

        ViewBag.Holidays =
            holidays;


        return View(holidays);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EmployeeHolidays(
    int? year,
    int? month)
    {
        var selectedYear =
            year ?? DateTime.Today.Year;

        var selectedMonth =
            month ?? DateTime.Today.Month;


        var firstDate =
            new DateTime(
                selectedYear,
                selectedMonth,
                1);

        var lastDate =
            firstDate
                .AddMonths(1)
                .AddDays(-1);


        var holidays =
            await _context.Holiday
                .Where(x =>
                    x.HolidayDate >= firstDate &&
                    x.HolidayDate <= lastDate &&
                    x.Status == "Published")
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();


        ViewBag.Year =
            selectedYear;

        ViewBag.Month =
            selectedMonth;

        return View(
            "EmployeeHolidays",
            holidays);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkHoliday(
    DateTime HolidayDate,
    string HolidayName,
    string HolidayType,
    string? Description)
    {
        if (!User.IsInRole("HR"))
            return Forbid();


        // =====================================================
        // NORMALIZE DATE
        // =====================================================

        HolidayDate =
            HolidayDate.Date;


        // =====================================================
        // VALIDATE DATE
        // =====================================================

        if (HolidayDate == DateTime.MinValue)
        {
            TempData["Error"] =
                "Please select a valid holiday date.";

            return RedirectToAction(
                nameof(Holidays),
                new
                {
                    year = DateTime.Today.Year,
                    month = DateTime.Today.Month
                });
        }


        // =====================================================
        // VALIDATE NAME
        // =====================================================

        if (string.IsNullOrWhiteSpace(HolidayName))
        {
            TempData["Error"] =
                "Please enter the holiday name.";

            return RedirectToAction(
                nameof(Holidays),
                new
                {
                    year = HolidayDate.Year,
                    month = HolidayDate.Month
                });
        }


        // =====================================================
        // CHECK DUPLICATE
        // =====================================================

        var exists =
            await _context.Holiday
                .AnyAsync(x =>
                    x.HolidayDate == HolidayDate);


        if (exists)
        {
            TempData["Error"] =
                "A holiday already exists for this date.";

            return RedirectToAction(
                nameof(Holidays),
                new
                {
                    year = HolidayDate.Year,
                    month = HolidayDate.Month
                });
        }


        // =====================================================
        // CURRENT USER
        // =====================================================

        var currentUser =
            await _userManager.GetUserAsync(User);


        // =====================================================
        // CREATE
        // =====================================================

        var holiday =
            new Holiday
            {
                HolidayDate =
                    HolidayDate,

                HolidayName =
                    HolidayName.Trim(),

                HolidayType =
                    string.IsNullOrWhiteSpace(HolidayType)
                        ? "Public Holiday"
                        : HolidayType,

                Description =
                    Description?.Trim(),

                Status =
                    "Published",

                CreatedDate =
                    DateTime.Now,

                CreatedBy =
                    currentUser?.UserName
            };


        _context.Holiday.Add(holiday);

        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Holiday marked successfully.";


        return RedirectToAction(
            nameof(Holidays),
            new
            {
                year = HolidayDate.Year,
                month = HolidayDate.Month
            });
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHoliday(
    int HolidayId,
    DateTime HolidayDate,
    string HolidayName,
    string HolidayType,
    string? Description)
    {
        if (!User.IsInRole("HR"))
            return Forbid();


        var holiday =
            await _context.Holiday
                .FirstOrDefaultAsync(x =>
                    x.HolidayId == HolidayId);


        if (holiday == null)
        {
            TempData["Error"] =
                "Holiday not found.";

            return RedirectToAction(
                nameof(Holidays));
        }


        HolidayDate =
            HolidayDate.Date;


        var duplicate =
            await _context.Holiday
                .AnyAsync(x =>
                    x.HolidayId != HolidayId &&
                    x.HolidayDate == HolidayDate);


        if (duplicate)
        {
            TempData["Error"] =
                "Another holiday already exists for this date.";

            return RedirectToAction(
                nameof(Holidays),
                new
                {
                    year = HolidayDate.Year,
                    month = HolidayDate.Month
                });
        }


        holiday.HolidayDate =
            HolidayDate;

        holiday.HolidayName =
            HolidayName.Trim();

        holiday.HolidayType =
            HolidayType;

        holiday.Description =
            Description?.Trim();


        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Holiday updated successfully.";


        return RedirectToAction(
            nameof(Holidays),
            new
            {
                year = HolidayDate.Year,
                month = HolidayDate.Month
            });
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateHoliday(
    int id)
    {
        if (!User.IsInRole("HR"))
            return Forbid();


        var holiday =
            await _context.Holiday
                .FirstOrDefaultAsync(x =>
                    x.HolidayId == id);


        if (holiday == null)
        {
            TempData["Error"] =
                "Holiday not found.";

            return RedirectToAction(
                nameof(Holidays));
        }


        holiday.Status =
            "Inactive";


        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Holiday deactivated successfully.";


        return RedirectToAction(
            nameof(Holidays),
            new
            {
                year = holiday.HolidayDate.Year,
                month = holiday.HolidayDate.Month
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        // =====================================================
        // HR ONLY
        // =====================================================

        if (!User.IsInRole("HR"))
            return Forbid();


        // =====================================================
        // FIND HOLIDAY
        // =====================================================

        var holiday =
            await _context.Holiday
                .FirstOrDefaultAsync(x =>
                    x.HolidayId == id);


        if (holiday == null)
        {
            TempData["Error"] =
                "Holiday not found.";

            return RedirectToAction(
                nameof(Holidays),
                new
                {
                    year = DateTime.Today.Year,
                    month = DateTime.Today.Month
                });
        }


        // =====================================================
        // SAVE DATE BEFORE DELETE
        // =====================================================

        var year =
            holiday.HolidayDate.Year;

        var month =
            holiday.HolidayDate.Month;


        // =====================================================
        // DELETE
        // =====================================================

        _context.Holiday.Remove(holiday);

        await _context.SaveChangesAsync();


        // =====================================================
        // SUCCESS
        // =====================================================

        TempData["Success"] =
            "Holiday deleted successfully.";


        return RedirectToAction(
            nameof(Holidays),
            new
            {
                year = year,
                month = month
            });
    }
}
