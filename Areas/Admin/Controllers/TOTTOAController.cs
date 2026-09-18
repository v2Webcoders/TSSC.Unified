using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;

namespace TSSC.Unified.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TOTTOAController :Controller
    {
        private readonly AppdbContext _db;

        public TOTTOAController(AppdbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> PaymentUpdate()
        {
            var trainers = await _db.TrainerRegistration
                .AsNoTracking()
                .Where(x =>
                    x.PaymentStatus == "Success" && x.IsActive != false &&
                    !x.PaymentVerified)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            return View(trainers);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(int id)
        {
            var trainer = await _db.TrainerRegistration
                .FirstOrDefaultAsync(x => x.Id == id);

            if (trainer == null)
            {
                TempData["error"] = "Trainer not found.";
                return RedirectToAction(nameof(PaymentUpdate));
            }

            if (!string.Equals(
                    trainer.PaymentStatus,
                    "Success",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Payment has not been completed by the trainer.";

                return RedirectToAction(
                    nameof(PaymentUpdate),
                    new { registrationNo = trainer.RegistrationNo });
            }

            trainer.PaymentVerified = true;
            trainer.UpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["msg"] = "Payment verified successfully.";

            return RedirectToAction(
                nameof(PaymentUpdate),
                new { registrationNo = trainer.RegistrationNo });
        }
        [HttpGet]
        public async Task<IActionResult> VerifiedTrainers()
        {
            var trainers = await _db.TrainerRegistration
                .AsNoTracking()
                .Where(x =>
                    x.PaymentStatus == "Success" && x.IsActive &&
                    x.PaymentVerified)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            return View(trainers);
        }
    }
}
