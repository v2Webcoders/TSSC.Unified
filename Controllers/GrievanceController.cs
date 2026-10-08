using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
namespace TSSC.Unified.Controllers
{
    [AllowAnonymous]
  
    public class GrievanceController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppdbContext _context;
        private readonly UtilityService _us;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GrievanceController> _logger;
        private readonly IPasswordHasher<string> _passwordHasher;

        private const int OtpExpiryMinutes = 10;
        private const int MaxOtpAttempts = 5;
        private const int MaxOtpRequestsPerHour = 5;

        private const long MaxAttachmentSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
        };

        public GrievanceController(
            UtilityService us,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            AppdbContext context,
            EmailService emailService,
            IWebHostEnvironment environment,
            ILogger<GrievanceController> logger,
            IPasswordHasher<string> passwordHasher)
        {
            _us = us;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _environment = environment;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }
        private static string NormalizeEmail(string? email)
        {
            return (email ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
        }

        private static string NormalizeMobile(string? mobile)
        {
            return new string(
                (mobile ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return new System.ComponentModel.DataAnnotations
                .EmailAddressAttribute()
                .IsValid(email);
        }

        private async Task<string> GenerateUniqueGrievanceNumberAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                var random =
                    RandomNumberGenerator.GetInt32(
                        100000,
                        1000000);

                var grievanceNo =
                    $"GRV-{DateTime.UtcNow:yyyyMMdd}-{random}";

                var exists =
                    await _context.Grievance
                        .AnyAsync(x =>
                            x.GrievanceNo == grievanceNo);

                if (!exists)
                {
                    return grievanceNo;
                }
            }

            throw new InvalidOperationException(
                "Unable to generate grievance number.");
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult CreateGrievance()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmailOtp(string email)
        {
            try
            {
                email = NormalizeEmail(email);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please enter your email address."
                    });
                }

                if (!IsValidEmail(email))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please enter a valid email address."
                    });
                }

                // Maximum 5 OTP requests in one hour
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);

                var requestCount = await _context.GrievanceEmailVerification
                    .CountAsync(x =>
                        x.Email == email &&
                        x.CreatedDate >= oneHourAgo);

                if (requestCount >= 5)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Too many OTP requests. Please try again later."
                    });
                }

                // Invalidate previous OTPs
                var oldOtps = await _context.GrievanceEmailVerification
                    .Where(x =>
                        x.Email == email &&
                        !x.IsUsed &&
                        !x.IsVerified)
                    .ToListAsync();

                foreach (var oldOtp in oldOtps)
                {
                    oldOtp.IsUsed = true;
                    oldOtp.UsedDate = DateTime.UtcNow;
                }

                // Generate secure 6 digit OTP
                var otp = RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString();

                // Never store plain OTP
                var otpHash = _passwordHasher.HashPassword(
                    email,
                    otp);

                var verification = new GrievanceEmailVerification
                {
                    Email = email,
                    OtpHash = otpHash,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    IsVerified = false,
                    IsUsed = false,
                    AttemptCount = 0,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.GrievanceEmailVerification
                    .AddAsync(verification);

                await _context.SaveChangesAsync();

                // Existing single OTP template
                var templatePath = Path.Combine(
                    _environment.WebRootPath,
                    "EmailTemplates",
                    "GrievanceEmailVerification.html");

                if (!System.IO.File.Exists(templatePath))
                {
                    _logger.LogError(
                        "OTP template not found: {TemplatePath}",
                        templatePath);

                    verification.IsUsed = true;
                    verification.UsedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = false,
                        message = "Email service is temporarily unavailable."
                    });
                }

                var replacements = new Dictionary<string, string>
        {
            { "BaseUrl", $"{Request.Scheme}://{Request.Host}" },
            { "Email", email },
            { "OTP", otp },
            { "ExpiryMinutes", "10" }
        };

                var emailResult = await _emailService.SendEmailAsync(
                    email,
                    "Grievance Email Verification OTP - TSSC",
                    templatePath,
                    replacements);

                if (string.IsNullOrWhiteSpace(emailResult))
                {
                    verification.IsUsed = true;
                    verification.UsedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogError(
                        "OTP email failed for {Email}",
                        email);

                    return Json(new
                    {
                        success = false,
                        message = "Unable to send OTP. Please try again."
                    });
                }

                _logger.LogInformation(
                    "Grievance OTP sent to {Email}",
                    email);

                return Json(new
                {
                    success = true,
                    message = "OTP sent successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while sending grievance OTP.");

                return Json(new
                {
                    success = false,
                    message = "Something went wrong while sending OTP."
                });
            }
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmailOtp(string email, string otp)
        {
            try
            {
                email = NormalizeEmail(email);
                otp = otp?.Trim() ?? string.Empty;

                if (!IsValidEmail(email))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid email address."
                    });
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        otp,
                        @"^\d{6}$"))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please enter a valid 6 digit OTP."
                    });
                }

                var verification =
                    await _context.GrievanceEmailVerification
                        .Where(x =>
                            x.Email == email &&
                            !x.IsUsed)
                        .OrderByDescending(x => x.CreatedDate)
                        .FirstOrDefaultAsync();

                if (verification == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "OTP not found. Please request a new OTP."
                    });
                }

                if (verification.IsVerified)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Email is already verified."
                    });
                }

                if (verification.ExpiresAt <= DateTime.UtcNow)
                {
                    verification.IsUsed = true;
                    verification.UsedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = false,
                        message = "OTP has expired. Please request a new OTP."
                    });
                }

                if (verification.AttemptCount >= 5)
                {
                    verification.IsUsed = true;
                    verification.UsedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = false,
                        message = "Maximum attempts exceeded. Please request a new OTP."
                    });
                }

                // Count every attempt
                verification.AttemptCount++;

                var result = _passwordHasher.VerifyHashedPassword(
                    email,
                    verification.OtpHash,
                    otp);

                if (result == PasswordVerificationResult.Failed)
                {
                    await _context.SaveChangesAsync();

                    var remaining =
                        5 - verification.AttemptCount;

                    return Json(new
                    {
                        success = false,
                        message = remaining > 0
                            ? $"Invalid OTP. {remaining} attempt(s) remaining."
                            : "Invalid OTP. Please request a new OTP."
                    });
                }

                // Successful verification
                verification.IsVerified = true;
                verification.VerifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Grievance email verified: {Email}",
                    email);

                return Json(new
                {
                    success = true,
                    message = "Email verified successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while verifying grievance OTP.");

                return Json(new
                {
                    success = false,
                    message = "Something went wrong while verifying OTP."
                });
            }
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitGrievance(GrievanceViewModel model)
        {
            string? physicalFilePath = null;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!ModelState.IsValid)
                {
                    return View("CreateGrievance", model);
                }

                // Normalize input
                model.Name = model.Name?.Trim() ?? string.Empty;
                model.Email = NormalizeEmail(model.Email);
                model.MobileNumber = NormalizeMobile(model.MobileNumber);
                model.Description =
                    model.Description?.Trim() ?? string.Empty;

                // Server-side validation
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(
                        nameof(model.Name),
                        "Name is required.");
                }

                if (!IsValidEmail(model.Email))
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "Please enter a valid email address.");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        model.MobileNumber,
                        @"^\d{10}$"))
                {
                    ModelState.AddModelError(
                        nameof(model.MobileNumber),
                        "Please enter a valid 10 digit mobile number.");
                }

                if (string.IsNullOrWhiteSpace(model.Description))
                {
                    ModelState.AddModelError(
                        nameof(model.Description),
                        "Grievance description is required.");
                }

                if (!ModelState.IsValid)
                {
                    return View("CreateGrievance", model);
                }
                var verifiedOtp =
                    await _context.GrievanceEmailVerification
                        .Where(x =>
                            x.Email == model.Email &&
                            x.IsVerified &&
                            !x.IsUsed &&
                            x.ExpiresAt > DateTime.UtcNow)
                        .OrderByDescending(x => x.VerifiedDate)
                        .FirstOrDefaultAsync();

                if (verifiedOtp == null)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "Please verify your email address before submitting.");

                    return View("CreateGrievance", model);
                }

                // Attachment validation
                if (model.Attachment != null)
                {
                    if (model.Attachment.Length <= 0)
                    {
                        ModelState.AddModelError(
                            nameof(model.Attachment),
                            "Invalid attachment.");
                    }

                    if (model.Attachment.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(
                            nameof(model.Attachment),
                            "Attachment cannot exceed 5 MB.");
                    }

                    var extension =
                        Path.GetExtension(model.Attachment.FileName)
                            .ToLowerInvariant();

                    var allowedExtensions =
                        new[] { ".pdf", ".jpg", ".jpeg", ".png" };

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            nameof(model.Attachment),
                            "Only PDF, JPG, JPEG and PNG files are allowed.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View("CreateGrievance", model);
                }

                // Generate grievance number
                var grievanceNo =
                    await GenerateUniqueGrievanceNumberAsync();

                // Save attachment
                string? attachmentPath = null;

                if (model.Attachment != null)
                {
                    var uploadFolder = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "grievances");

                    Directory.CreateDirectory(uploadFolder);

                    var extension =
                        Path.GetExtension(model.Attachment.FileName)
                            .ToLowerInvariant();

                    var fileName =
                        $"{Guid.NewGuid():N}{extension}";

                    physicalFilePath =
                        Path.Combine(
                            uploadFolder,
                            fileName);

                    await using var stream =
                        new FileStream(
                            physicalFilePath,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None,
                            81920,
                            useAsync: true);

                    await model.Attachment.CopyToAsync(stream);

                    attachmentPath =
                        $"/uploads/grievances/{fileName}";
                }

                // Create grievance
                var grievance = new Grievance
                {
                    GrievanceNo = grievanceNo,
                    Name = model.Name,
                    MobileNumber = model.MobileNumber,
                    Email = model.Email,
                    Description = model.Description,
                    Status = "Submitted",
                    Priority = "Normal",
                    AttachmentPath = attachmentPath,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                await _context.Grievance.AddAsync(grievance);

                // Consume OTP
                verifiedOtp.IsUsed = true;
                verifiedOtp.UsedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                var templatePath = Path.Combine(
                    _environment.WebRootPath,
                    "EmailTemplates",
                    "GrievanceSubmitted.html"
                );

                var replacements = new Dictionary<string, string>
                {
                    { "Name", grievance.Name },
                    { "GrievanceNo", grievance.GrievanceNo },
                    { "Email", grievance.Email },
                    { "MobileNumber", grievance.MobileNumber },
                    { "Status", grievance.Status },
                    { "Priority", grievance.Priority },
                    { "CreatedDate", grievance.CreatedDate.ToString("dd-MMM-yyyy hh:mm tt") },
                    { "Description", grievance.Description }
                };

                await _emailService.SendEmailAsync(
                    grievance.Email,
                    "Grievance Submitted Successfully - TSSC",
                    templatePath,
                    replacements
                );

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Grievance {GrievanceNo} submitted successfully.",
                    grievanceNo);

                TempData["GrievanceNo"] = grievanceNo;

                return RedirectToAction(nameof(GrievanceSuccess));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Delete uploaded file if DB transaction failed
                if (!string.IsNullOrWhiteSpace(physicalFilePath) &&
                    System.IO.File.Exists(physicalFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(physicalFilePath);
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogWarning(
                            fileEx,
                            "Could not delete orphan grievance file.");
                    }
                }

                _logger.LogError(
                    ex,
                    "Error while submitting grievance.");

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to submit your grievance. Please try again.");

                return View("CreateGrievance", model);
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GrievanceSuccess()
        {
            var grievanceNo =
                TempData["GrievanceNo"]?.ToString();

            if (string.IsNullOrWhiteSpace(grievanceNo))
            {
                return RedirectToAction(
                    nameof(CreateGrievance));
            }

            ViewBag.GrievanceNo = grievanceNo;

            return View();
        }
    }
}
