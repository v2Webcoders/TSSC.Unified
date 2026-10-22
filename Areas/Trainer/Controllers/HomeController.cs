using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using System.Diagnostics;


namespace TSSC.Unified.Areas.Trainer.Controllers
{

    [Area("Trainer")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppdbContext _db;
        private readonly UserManager<AppUser> _userManager;
        public HomeController(ILogger<HomeController> logger, AppdbContext db, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return NotFound("Trainer email not found.");
            }

            var email = user.Email.Trim().ToLowerInvariant();

            var registration = await _db.TrainerRegistration
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Email != null &&
                    x.Email.Trim().ToLower() == email);

            if (registration == null)
            {
                return NotFound("Trainer registration not found.");
            }
            // Job Role
            var jobRoleTitle = await _db.JobRoles
                .AsNoTracking()
                .Where(x => x.Id == registration.JobRoleId)
                .Select(x => x.JobRoleTitle)
                .FirstOrDefaultAsync();

            // Latest screening
            var screening = await _db.ScreeningSchedule
                .AsNoTracking()
                .Where(x => x.TrainerRegistrationId == registration.Id)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            // Latest batch mapping
            var batchMapping = await _db.BatchMasterTrainers
                .AsNoTracking()
                .Include(x => x.Batch)
                .Where(x => x.TrainerRegistrationId == registration.Id)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            var batch = batchMapping?.Batch;

            // Screening status
            var screeningStatus = screening?.Status?.Trim() ?? "";

            bool hasScreening = screening != null;

            bool screeningSuccess = string.Equals(
                screeningStatus,
                "Success",
                StringComparison.OrdinalIgnoreCase);

            bool screeningRejected = string.Equals(
                screeningStatus,
                "Rejected",
                StringComparison.OrdinalIgnoreCase);

            bool screeningOnHold = string.Equals(
                screeningStatus,
                "On Hold",
                StringComparison.OrdinalIgnoreCase);

            // Payment status
            bool paymentPaid = string.Equals(
                registration.PaymentStatus?.Trim(),
                "Success",
                StringComparison.OrdinalIgnoreCase);

            bool paymentVerified = registration.PaymentVerified;
            bool financeApproved = registration.FinanceApproved;

            bool paymentCompleted =
                paymentPaid && paymentVerified && financeApproved;

            // Batch result and certificate
            bool resultUploaded =
                batchMapping != null && batchMapping.ResultUploaded;

            bool certificateUploaded = registration.CertificateUploaded;
            bool certificateRequestRaised = registration.RaiseReq;

            // Screening display
            string screeningText;

            if (!hasScreening)
            {
                screeningText = "Awaiting schedule";
            }
            else
            {
                screeningText = screeningStatus;

                if (screening != null)
                {
                    screeningText +=
                        $" · {screening.ScreeningDate:dd MMM yyyy}";
                }
            }

            // Payment display
            string paymentText;

            if (!screeningSuccess)
            {
                paymentText = screeningRejected
                    ? "Not applicable — screening rejected"
                    : "Pending screening success";
            }
            else if (!paymentPaid)
            {
                paymentText = "Payment pending";
            }
            else if (!paymentVerified)
            {
                paymentText = "Verification pending";
            }
            else if (!financeApproved)
            {
                paymentText = "Finance approval pending";
            }
            else
            {
                paymentText = "Payment approved";
            }

            // Certificate display
            string certificateText;

            if (certificateUploaded)
            {
                certificateText = "Certificate available";
            }
            else if (!paymentCompleted)
            {
                certificateText = "Pending payment approval";
            }
            else if (batch == null)
            {
                certificateText = "Awaiting batch assignment";
            }
            else if (!resultUploaded)
            {
                certificateText = "Result pending";
            }
            else if (!certificateRequestRaised)
            {
                certificateText = "Raise certificate request";
            }
            else
            {
                certificateText = "Certificate pending";
            }

            // Current stage and next action
            string currentStage;
            string nextStep;

            if (screeningRejected)
            {
                currentStage = "Screening — Rejected";
                nextStep = "Please contact TSSC for information about your application.";
            }
            else if (screeningOnHold)
            {
                currentStage = "Screening — On Hold";
                nextStep = "Your screening is on hold. Please wait for further instructions from TSSC.";
            }
            else if (!hasScreening)
            {
                currentStage = "Screening — Awaiting Schedule";
                nextStep = "TSSC will schedule your screening and share the date, time and venue or meeting link.";
            }
            else if (!screeningSuccess)
            {
                currentStage = "Screening — Scheduled";
                nextStep = "Attend your screening at the scheduled date and time.";
            }
            else if (!paymentPaid)
            {
                currentStage = "Payment — Pending";
                nextStep = "Complete your payment to proceed to verification and finance approval.";
            }
            else if (!paymentVerified)
            {
                currentStage = "Payment — Verification Pending";
                nextStep = "Your payment is submitted and is awaiting verification.";
            }
            else if (!financeApproved)
            {
                currentStage = "Payment — Finance Approval Pending";
                nextStep = "Your payment is awaiting finance approval.";
            }
            else if (batch == null)
            {
                currentStage = "Batch — Awaiting Assignment";
                nextStep = "Your payment is approved. Please wait for batch assignment.";
            }
            else if (!resultUploaded)
            {
                currentStage = "Result — Pending";
                nextStep = "Your batch result has not been uploaded yet.";
            }
            else if (!certificateRequestRaised)
            {
                currentStage = "Certificate Request — Required";
                nextStep = "Raise your certificate request from the Application Status page.";
            }
            else if (!certificateUploaded)
            {
                currentStage = "Certificate — Pending";
                nextStep = "Your certificate request has been raised. Please wait for certificate upload.";
            }
            else
            {
                currentStage = "Certified — Certificate Available";
                nextStep = "Your certificate is available. Open Application Status to download it.";
            }

            // Data for the dashboard
            ViewBag.JobRoleTitle = jobRoleTitle ?? "Not specified";
            ViewBag.RegistrationDateText =
                registration.CreatedDate.ToString("dd MMM yyyy");

            ViewBag.HasScreening = hasScreening;
            ViewBag.ScreeningSuccess = screeningSuccess;
            ViewBag.ScreeningRejected = screeningRejected;
            ViewBag.ScreeningOnHold = screeningOnHold;
            ViewBag.ScreeningText = screeningText;

            ViewBag.PaymentPaid = paymentPaid;
            ViewBag.PaymentVerified = paymentVerified;
            ViewBag.FinanceApproved = financeApproved;
            ViewBag.PaymentCompleted = paymentCompleted;
            ViewBag.PaymentText = paymentText;

            ViewBag.BatchAssigned = batch != null;
            ViewBag.BatchCode = batch?.BatchCode ?? "-";
            ViewBag.BatchName = batch?.BatchName ?? "-";

            ViewBag.ResultUploaded = resultUploaded;
            ViewBag.CertificateUploaded = certificateUploaded;
            ViewBag.CertificateText = certificateText;

            ViewBag.CurrentStage = currentStage;
            ViewBag.NextStep = nextStep;
            return View(registration);
            // return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
