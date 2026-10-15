using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QUIZAPP.Models;
using QUIZAPP;
using TSSC.Unified.Services;
using TSSC.Unified.ViewModel;
using TSSC.Unified.Models;
using System.Security.Cryptography;
using QUIZAPP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using ClosedXML.Excel;
using iText.Barcodes;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using System.Globalization;

namespace TSSC.Unified.Areas.TP.Controllers
{
    [Area("TP")]
    public class RegistrationController : Controller
    {
        private bool IsAjax =>
           Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        private readonly AppdbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;
        private readonly ILogger<RegistrationController> _logger;
        public RegistrationController(
        AppdbContext context,
        IWebHostEnvironment environment,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager, EmailService emailService, ILogger<RegistrationController> logger)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
        }
        [HttpGet]
        public IActionResult Create(string? membershipType)
        {
            var allowedTypes = new[]
            {
                "Associate Member",
                "Industry Member",
                "Platinum Industry Member",
                "Academia Member"
            };

            if (string.IsNullOrWhiteSpace(membershipType) ||
                !allowedTypes.Contains(membershipType))
            {
                return BadRequest("Invalid membership type.");
            }

            var model = new TPRegistrationVM
            {
                MembershipType = membershipType
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        public async Task<IActionResult> Create(TPRegistrationVM model)
        {
            var rawDate = Request.Form[nameof(model.DateOfIncorporation)].ToString();

            if (!DateTime.TryParseExact(
                    rawDate,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var incorporationDate))
            {
                ModelState.AddModelError(
                    nameof(model.DateOfIncorporation),
                    "Enter a valid date in DD/MM/YYYY format.");
            }
            else
            {
                model.DateOfIncorporation = incorporationDate;
            }

            if (!ModelState.IsValid)
                return InvalidResult(model);

            const long maxPdfSize = 2 * 1024 * 1024; // 2 MB

            var uploadedFiles = new (IFormFile? File, string FieldName, string Label)[]
            {
        (model.FinancialDocuments, nameof(model.FinancialDocuments), "Financial Documents"),
        (model.RegistrationCertificate, nameof(model.RegistrationCertificate), "Registration Certificate"),
        (model.PANCardCopy, nameof(model.PANCardCopy), "PAN Card Copy"),
        (model.GSTCertificate, nameof(model.GSTCertificate), "GST Certificate"),
        (model.AddressProof, nameof(model.AddressProof), "Address Proof"),
        (model.CancelledCheque, nameof(model.CancelledCheque), "Cancelled Cheque"),
        (model.UndertakingDocument, nameof(model.UndertakingDocument), "Undertaking"),
        (model.AuthorizedPersonIdProof, nameof(model.AuthorizedPersonIdProof), "Authorized Person ID Proof")
            };

            foreach (var (file, fieldName, label) in uploadedFiles)
            {
                if (file == null || file.Length == 0)
                    continue;

                var extension = Path.GetExtension(file.FileName);

                var allowed = fieldName is nameof(model.CancelledCheque)
                                        or nameof(model.AuthorizedPersonIdProof)
                    ? new[] { ".pdf", ".jpg", ".jpeg", ".png" }
                    : new[] { ".pdf" };

                if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(fieldName, $"{label}: file type not allowed.");
                    continue;
                }

                if (!await HasValidSignatureAsync(file, extension))
                {
                    ModelState.AddModelError(fieldName, $"{label}: file content does not match its type.");
                    continue;
                }

                if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) &&
                    file.Length > maxPdfSize)
                {
                    ModelState.AddModelError(fieldName, $"{label} PDF must not exceed 2 MB.");
                }
            }

            if (!ModelState.IsValid)
                return InvalidResult(model);

            if (!await _roleManager.RoleExistsAsync("TrainingPartner"))
            {
                ModelState.AddModelError("", "TrainingPartner role is not configured.");
                return InvalidResult(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email),
                    "An account already exists with this email address.");
                return InvalidResult(model);
            }

            TPRegistration? application = null;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                application = new TPRegistration
                {
                    MembershipType = model.MembershipType,
                    OrganizationName = model.OrganizationName,
                    OrganizationType = model.OrganizationType,
                    RegistrationNumber = model.RegistrationNumber,
                    DateOfIncorporation = model.DateOfIncorporation,
                    PANNumber = model.PANNumber.ToUpperInvariant(),

                    IsGSTAvailable = model.IsGSTAvailable,
                    GSTNumber = model.IsGSTAvailable
                        ? model.GSTNumber?.Trim().ToUpperInvariant()
                        : null,

                    SectorExperience = model.SectorExperience,
                    YearsOfExperience = model.YearsOfExperience,
                    PreviousProjects = model.PreviousProjects,
                    TrainingCentersCount = model.TrainingCentersCount,
                    TrainersAvailableCount = model.TrainersAvailableCount,

                    RegisteredOfficeAddress = model.RegisteredOfficeAddress,
                    State = model.State,
                    District = model.District,
                    City = model.City,
                    PinCode = model.PinCode,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    AlternateContactPerson = model.AlternateContactPerson,

                    AnnualTurnoverLast3Years = model.AnnualTurnoverLast3Years,
                    BankAccountNumber = model.BankAccountNumber,
                    IFSCCode = model.IFSCCode,
                    BankName = model.BankName,

                    AuthorizedSignatoryName = model.AuthorizedSignatoryName,
                    Designation = model.Designation,
                    AuthorizedPersonEmail = model.AuthorizedPersonEmail,
                    AuthorizedPersonMobile = model.AuthorizedPersonMobile,

                    DeclarationAccepted = model.DeclarationAccepted,

                    Status = "Submitted",
                    OverallApprovalStatus = "In_Progress",
                    CurrentStage = "REGIONAL",

                    CreatedOn = DateTime.UtcNow
                };

                _context.TPRegistrations.Add(application);
                await _context.SaveChangesAsync();

                foreach (var (file, _, label) in uploadedFiles.Where(x => x.File != null && x.File.Length > 0))
                {
                    var savedFileName = await SaveFileAsync(file!, application.Id, label);

                    _context.TPRegistrationDocuments.Add(new TPRegistrationDocument
                    {
                        TPRegistrationId = application.Id,
                        DocumentType = label,
                        FileName = savedFileName,
                        UploadedOn = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                var temporaryPassword = GenerateTemporaryPassword();
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.MobileNumber,
                    EmailConfirmed = false,
                    Password = temporaryPassword
                };

                var createUserResult = await _userManager.CreateAsync(user, temporaryPassword);
                if (!createUserResult.Succeeded)
                    throw new InvalidOperationException(
                        string.Join(" ", createUserResult.Errors.Select(x => x.Description)));

                var addRoleResult = await _userManager.AddToRoleAsync(user, "TrainingPartner");
                if (!addRoleResult.Succeeded)
                    throw new InvalidOperationException(
                        string.Join(" ", addRoleResult.Errors.Select(x => x.Description)));

                application.ApplicationUserId = user.Id;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Send only after everything is committed.
                try
                {
                    var templatePath = Path.Combine(
                        _environment.WebRootPath, "emailtemplates", "tp_registration-submitted.html");

                    await _emailService.SendEmailAsync(
                        user.Email!,
                        "TSSC TP Registration Submitted Successfully",
                        templatePath,
                        new Dictionary<string, string>
                        {
                            ["Name"] = application.AuthorizedSignatoryName,
                            ["OrganizationName"] = application.OrganizationName,
                            ["UserName"] = user.Email!,
                            ["Password"] = temporaryPassword,
                            ["ApplicationId"] = application.Id.ToString()
                        });
                }
                catch (Exception emailException)
                {
                    _logger.LogError(emailException,
                        "TP Registration {ApplicationId} saved, but the acknowledgement email failed.",
                        application.Id);
                }

                TempData["Success"] =
                    "TP registration submitted successfully. Your account has been created.";

                if (IsAjax)
                    return Json(new { redirectUrl = Url.Action(nameof(Confirmation)) });

                return RedirectToAction(nameof(Confirmation));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                if (application != null)
                {
                    try
                    {
                        var uploadFolder = Path.Combine(
                            _environment.WebRootPath, "uploads", "tp-registrations",
                            application.Id.ToString());

                        if (Directory.Exists(uploadFolder))
                            Directory.Delete(uploadFolder, recursive: true);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Upload cleanup failed for {Id}", application.Id);
                    }
                }

                _logger.LogError(ex, "TP registration failed.");

                // Shows the real error only when running in Development
                var message = _environment.IsDevelopment()
                    ? (ex.InnerException?.Message ?? ex.Message)
                    : "Registration could not be submitted. Please try again.";

                ModelState.AddModelError("", message);
                return InvalidResult(model);
            }
        }

        // ---------- helpers: add to the controller ----------

        private IActionResult InvalidResult(TPRegistrationVM model)
        {
            if (!IsAjax)
                return View(model);

            var errors = ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            return BadRequest(new { errors });
        }

        private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension)
        {
            var header = new byte[8];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
            if (read < 4) return false;

            return extension.ToLowerInvariant() switch
            {
                ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46, // %PDF
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                _ => false
            };
        }

        //[HttpPost]
        //[RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        //public async Task<IActionResult> Create(TPRegistrationVM model)
        //{
        //    if (!ModelState.IsValid)
        //        return View(model);

        //    const long maxPdfSize = 2 * 1024 * 1024; // 2 MB

        //    var uploadedFiles = new (IFormFile? File, string FieldName, string Label)[]
        //    {
        //    (model.FinancialDocuments, nameof(model.FinancialDocuments), "Financial Documents"),
        //    (model.RegistrationCertificate, nameof(model.RegistrationCertificate), "Registration Certificate"),
        //    (model.PANCardCopy, nameof(model.PANCardCopy), "PAN Card Copy"),
        //    (model.GSTCertificate, nameof(model.GSTCertificate), "GST Certificate"),
        //    (model.AddressProof, nameof(model.AddressProof), "Address Proof"),
        //    (model.CancelledCheque, nameof(model.CancelledCheque), "Cancelled Cheque"),
        //    (model.UndertakingDocument, nameof(model.UndertakingDocument), "Undertaking"),
        //    (model.AuthorizedPersonIdProof, nameof(model.AuthorizedPersonIdProof), "Authorized Person ID Proof")
        //    };

        //    foreach (var uploadedFile in uploadedFiles)
        //    {
        //        if (uploadedFile.File == null || uploadedFile.File.Length == 0)
        //            continue;

        //        var extension = Path.GetExtension(uploadedFile.File.FileName);

        //        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) &&
        //            uploadedFile.File.Length > maxPdfSize)
        //        {
        //            ModelState.AddModelError(
        //                uploadedFile.FieldName,
        //                $"{uploadedFile.Label} PDF must not exceed 2 MB.");
        //        }
        //    }

        //    if (!ModelState.IsValid)
        //        return View(model);

        //    if (!await _roleManager.RoleExistsAsync("TrainingPartner"))
        //    {
        //        ModelState.AddModelError("", "TrainingPartner role is not configured.");
        //        return View(model);
        //    }

        //    var existingUser = await _userManager.FindByEmailAsync(model.Email);
        //    if (existingUser != null)
        //    {
        //        ModelState.AddModelError(nameof(model.Email),
        //            "An account already exists with this email address.");
        //        return View(model);
        //    }

        //    TPRegistration? application = null;

        //    await using var transaction = await _context.Database.BeginTransactionAsync();

        //    try
        //    {
        //        application = new TPRegistration
        //        {
        //            OrganizationName = model.OrganizationName,
        //            OrganizationType = model.OrganizationType,
        //            RegistrationNumber = model.RegistrationNumber,
        //            DateOfIncorporation = model.DateOfIncorporation,
        //            PANNumber = model.PANNumber.ToUpperInvariant(),
        //            // GST
        //            IsGSTAvailable = model.IsGSTAvailable,
        //            GSTNumber = model.IsGSTAvailable
        //? model.GSTNumber?.Trim().ToUpperInvariant()
        //: null,

        //            SectorExperience = model.SectorExperience,
        //            YearsOfExperience = model.YearsOfExperience,
        //            PreviousProjects = model.PreviousProjects,
        //            TrainingCentersCount = model.TrainingCentersCount,
        //            TrainersAvailableCount = model.TrainersAvailableCount,

        //            RegisteredOfficeAddress = model.RegisteredOfficeAddress,
        //            State = model.State,
        //            District = model.District,
        //            City = model.City,
        //            PinCode = model.PinCode,
        //            Email = model.Email,
        //            MobileNumber = model.MobileNumber,
        //            AlternateContactPerson = model.AlternateContactPerson,

        //            AnnualTurnoverLast3Years = model.AnnualTurnoverLast3Years,
        //            BankAccountNumber = model.BankAccountNumber,
        //            IFSCCode = model.IFSCCode,
        //            BankName = model.BankName,

        //            AuthorizedSignatoryName = model.AuthorizedSignatoryName,
        //            Designation = model.Designation,
        //            AuthorizedPersonEmail = model.AuthorizedPersonEmail,
        //            AuthorizedPersonMobile = model.AuthorizedPersonMobile,

        //            DeclarationAccepted = model.DeclarationAccepted,
        //            Status = "Submitted",
        //            CreatedOn = DateTime.UtcNow
        //        };

        //        _context.TPRegistrations.Add(application);
        //        await _context.SaveChangesAsync();

        //        var documents = new List<(IFormFile? File, string DocumentType)>
        //{
        //    (model.FinancialDocuments, "Financial Documents"),
        //    (model.RegistrationCertificate, "Registration Certificate"),
        //    (model.PANCardCopy, "PAN Card Copy"),
        //    (model.GSTCertificate, "GST Certificate"),
        //    (model.AddressProof, "Address Proof"),
        //    (model.CancelledCheque, "Cancelled Cheque"),
        //    (model.UndertakingDocument, "Undertaking"),
        //    (model.AuthorizedPersonIdProof, "Authorized Person ID Proof")
        //};

        //        foreach (var document in documents.Where(x => x.File != null))
        //        {
        //            var savedFileName = await SaveFileAsync(
        //                document.File!,
        //                application.Id,
        //                document.DocumentType);

        //            _context.TPRegistrationDocuments.Add(new TPRegistrationDocument
        //            {
        //                TPRegistrationId = application.Id,
        //                DocumentType = document.DocumentType,
        //                FileName = savedFileName,
        //                UploadedOn = DateTime.UtcNow
        //            });
        //        }

        //        await _context.SaveChangesAsync();

        //        // Generate a secure temporary password, then send a password-set/reset link.
        //        var temporaryPassword = GenerateTemporaryPassword();
        //        var user = new AppUser
        //        {
        //            UserName = model.Email,
        //            Email = model.Email,
        //            PhoneNumber = model.MobileNumber,
        //            EmailConfirmed = false,
        //            Password=temporaryPassword
        //        };


        //        var createUserResult = await _userManager.CreateAsync(user, temporaryPassword);
        //        if (!createUserResult.Succeeded)
        //            throw new InvalidOperationException(
        //                string.Join(" ", createUserResult.Errors.Select(x => x.Description)));

        //        var addRoleResult = await _userManager.AddToRoleAsync(user, "TrainingPartner");
        //        if (!addRoleResult.Succeeded)
        //            throw new InvalidOperationException(
        //                string.Join(" ", addRoleResult.Errors.Select(x => x.Description)));

        //        application.ApplicationUserId = user.Id;
        //        await _context.SaveChangesAsync();

        //        await transaction.CommitAsync();


        //        // Send only after registration, documents, account, and role are committed.
        //        try
        //        {
        //            var templatePath = Path.Combine(
        //                _environment.WebRootPath,
        //                "email-templates",
        //                "tp_registration-submitted.html");

        //            await _emailService.SendEmailAsync(
        //                user.Email!,
        //                "TSSC TP Registration Submitted Successfully",
        //                templatePath,
        //                new Dictionary<string, string>
        //                {
        //                    ["Name"] = application.AuthorizedSignatoryName,
        //                    ["OrganizationName"] = application.OrganizationName,
        //                    ["UserName"] = user.Email!,
        //                    ["Password"] = temporaryPassword,
        //                    ["ApplicationId"] = application.Id.ToString()
        //                });
        //        }
        //        catch (Exception emailException)
        //        {
        //            // Do not roll back the successfully committed registration because SMTP failed.
        //            //_logger.LogError(
        //            //    emailException,
        //            //    "TP Registration {ApplicationId} completed, but the acknowledgement email failed.",
        //            //    application.Id);
        //        }

        //        TempData["Success"] =
        //            "TP registration submitted successfully. Your account has been created.";

        //        return RedirectToAction(nameof(Confirmation));
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();

        //        // Uploaded files are outside the database transaction, so remove them manually.
        //        if (application != null)
        //        {
        //            var uploadFolder = Path.Combine(
        //                _environment.WebRootPath,
        //                "uploads",
        //                "tp-registrations",
        //                application.Id.ToString());

        //            if (Directory.Exists(uploadFolder))
        //                Directory.Delete(uploadFolder, recursive: true);
        //        }

        //        ModelState.AddModelError("", $"Registration could not be submitted: {ex.Message}");
        //        return View(model);
        //    }
        //}

        public IActionResult Confirmation()
        {
            return View();
        }

        private async Task<string> SaveFileAsync(
            IFormFile file,
            int applicationId,
            string documentType)
        {
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Invalid file type.");

            if (file.Length > 10 * 1024 * 1024)
                throw new InvalidOperationException("File size cannot exceed 10 MB.");

            var folderPath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "tp-registrations",
                applicationId.ToString());

            Directory.CreateDirectory(folderPath);

            var savedFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folderPath, savedFileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return savedFileName;
        }

        private static string GenerateTemporaryPassword()
        {
            var upper = (char)('A' + RandomNumberGenerator.GetInt32(26));
            var lower = (char)('a' + RandomNumberGenerator.GetInt32(26));

            var digits = RandomNumberGenerator.GetInt32(100000, 1000000);

            return $"{upper}{lower}{digits}";
        }

        public async Task<IActionResult> TestRegistrationEmail(int id = 1)
        {
            var application = await _context.TPRegistrations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
                return NotFound("TP registration was not found.");

            AppUser? user = null;

            if (!string.IsNullOrWhiteSpace(application.ApplicationUserId))
            {
                user = await _userManager.FindByIdAsync(application.ApplicationUserId);
            }

            user ??= await _userManager.FindByEmailAsync(application.Email);

            var templatePath = Path.Combine(
                _environment.WebRootPath,
                "EmailTemplates",
                "tp_registration-submitted.html");

            await _emailService.SendEmailAsync(
                application.Email,
                "TP Registration Submitted Successfully",
                templatePath,
                new Dictionary<string, string>
                {
                    ["Name"] = application.AuthorizedSignatoryName,
                    ["OrganizationName"] = application.OrganizationName,
                    ["UserName"] = user?.Email ?? application.Email,

                    // Passwords cannot be read from ASP.NET Identity after creation.
                    ["Password"] = "Test email — password is not included",

                    ["ApplicationId"] = application.Id.ToString()
                });

            return Ok($"Test email sent successfully to {application.Email}");
        }

        public async Task<IActionResult> Details(int id)
        {
            var registration = await _context.TPRegistrations
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);



            ViewBag.UserRole = roles.FirstOrDefault();


            return View(registration);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var application = await _context.TPRegistrations
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.ApplicationUserId == userId);

            if (application == null)
                return NotFound();

            var canEdit =
            application.Regional_Head_Status == "Rejected" ||
            application.Affiliations_Status == "Rejected" ||
            application.Finance_Status == "Rejected" ||
            application.Vertical_Head_Status == "Rejected" ||
            application.SeniorReviewer1_Status == "Rejected" ||
            application.SeniorReviewer2_Status == "Rejected" ||
            application.CEO_Status == "Rejected" ||
            application.PaymentProcess_Status == "Rejected" ||
            application.CertificateIssuance_Status == "Rejected";

            if (!canEdit)
                return Forbid();

            var model = new TPRegistrationEditVM
            {
                Id = application.Id,
                MembershipType = application.MembershipType,
                OrganizationName = application.OrganizationName,
                OrganizationType = application.OrganizationType,
                RegistrationNumber = application.RegistrationNumber,
                DateOfIncorporation = application.DateOfIncorporation,
                PANNumber = application.PANNumber,
                IsGSTAvailable = application.IsGSTAvailable,
                GSTNumber = application.GSTNumber,

                SectorExperience = application.SectorExperience,
                YearsOfExperience = application.YearsOfExperience,
                PreviousProjects = application.PreviousProjects,
                TrainingCentersCount = application.TrainingCentersCount,
                TrainersAvailableCount = application.TrainersAvailableCount,

                RegisteredOfficeAddress = application.RegisteredOfficeAddress,
                State = application.State,
                District = application.District,
                City = application.City,
                PinCode = application.PinCode,
                Email = application.Email,
                MobileNumber = application.MobileNumber,
                AlternateContactPerson = application.AlternateContactPerson,

                AnnualTurnoverLast3Years = application.AnnualTurnoverLast3Years,
                BankAccountNumber = application.BankAccountNumber,
                IFSCCode = application.IFSCCode,
                BankName = application.BankName,

                AuthorizedSignatoryName = application.AuthorizedSignatoryName,
                Designation = application.Designation,
                AuthorizedPersonEmail = application.AuthorizedPersonEmail,
                AuthorizedPersonMobile = application.AuthorizedPersonMobile,

                DeclarationAccepted = application.DeclarationAccepted
            };

            ViewBag.Documents = await _context.TPRegistrationDocuments
                .Where(x => x.TPRegistrationId == application.Id)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        public async Task<IActionResult> Edit(TPRegistrationEditVM model)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // =====================================================
            // LOAD APPLICATION + VERIFY OWNERSHIP
            // =====================================================

            var application = await _context.TPRegistrations
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    x.ApplicationUserId == userId);

            if (application == null)
                return NotFound();

            // =====================================================
            // EDIT ONLY IF ANY STAGE IS REJECTED
            // =====================================================

            var canEdit =
            application.Regional_Head_Status == "Rejected" ||
            application.Affiliations_Status == "Rejected" ||
            application.Finance_Status == "Rejected" ||
            application.Vertical_Head_Status == "Rejected" ||
            application.SeniorReviewer1_Status == "Rejected" ||
            application.SeniorReviewer2_Status == "Rejected" ||
            application.CEO_Status == "Rejected" ||
            application.PaymentProcess_Status == "Rejected" ||
            application.CertificateIssuance_Status == "Rejected";

            if (!canEdit)
                return Forbid();

            // =====================================================
            // MODEL VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                ViewBag.Documents = await _context.TPRegistrationDocuments
                    .Where(x => x.TPRegistrationId == application.Id)
                    .ToListAsync();

                return View(model);
            }


            if (!model.DateOfIncorporation.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfIncorporation),
                    "Please enter a valid date of incorporation.");

                ViewBag.Documents = await _context.TPRegistrationDocuments
                    .Where(x => x.TPRegistrationId == application.Id)
                    .ToListAsync();

                return View(model);
            }

            const long maxPdfSize = 2 * 1024 * 1024; // 2 MB

            // =====================================================
            // FILE SIZE VALIDATION
            // =====================================================

            var uploadedFiles = new (IFormFile? File, string FieldName, string Label)[]
            {
        (
            model.FinancialDocuments,
            nameof(model.FinancialDocuments),
            "Financial Documents"
        ),
        (
            model.RegistrationCertificate,
            nameof(model.RegistrationCertificate),
            "Registration Certificate"
        ),
        (
            model.PANCardCopy,
            nameof(model.PANCardCopy),
            "PAN Card Copy"
        ),
        (
            model.GSTCertificate,
            nameof(model.GSTCertificate),
            "GST Certificate"
        ),
        (
            model.AddressProof,
            nameof(model.AddressProof),
            "Address Proof"
        ),
        (
            model.CancelledCheque,
            nameof(model.CancelledCheque),
            "Cancelled Cheque"
        ),
        (
            model.UndertakingDocument,
            nameof(model.UndertakingDocument),
            "Undertaking"
        ),
        (
            model.AuthorizedPersonIdProof,
            nameof(model.AuthorizedPersonIdProof),
            "Authorized Person ID Proof"
        )
            };

            foreach (var uploadedFile in uploadedFiles)
            {
                if (uploadedFile.File == null ||
                    uploadedFile.File.Length == 0)
                    continue;

                var extension =
                    Path.GetExtension(uploadedFile.File.FileName);

                if (extension.Equals(
                        ".pdf",
                        StringComparison.OrdinalIgnoreCase) &&
                    uploadedFile.File.Length > maxPdfSize)
                {
                    ModelState.AddModelError(
                        uploadedFile.FieldName,
                        $"{uploadedFile.Label} PDF must not exceed 2 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Documents = await _context.TPRegistrationDocuments
                    .Where(x => x.TPRegistrationId == application.Id)
                    .ToListAsync();

                return View(model);
            }

            // =====================================================
            // LOAD EXISTING DOCUMENTS
            // =====================================================

            var existingDocs = await _context.TPRegistrationDocuments
                .Where(x => x.TPRegistrationId == application.Id)
                .ToListAsync();

            // =====================================================
            // DOCUMENTS TO REPLACE
            // =====================================================

            var documentsToReplace = new List<(
                IFormFile File,
                string DocumentType,
                TPRegistrationDocument? ExistingDocument)>();

            void AddDocumentForReplacement(
                IFormFile? file,
                string documentType)
            {
                if (file == null || file.Length == 0)
                    return;

                var existingDocument = existingDocs
                    .FirstOrDefault(x =>
                        x.DocumentType == documentType);

                documentsToReplace.Add((
                    file,
                    documentType,
                    existingDocument));
            }

            AddDocumentForReplacement(
                model.FinancialDocuments,
                "Financial Documents");

            AddDocumentForReplacement(
                model.RegistrationCertificate,
                "Registration Certificate");

            AddDocumentForReplacement(
                model.PANCardCopy,
                "PAN Card Copy");

            AddDocumentForReplacement(
                model.GSTCertificate,
                "GST Certificate");

            AddDocumentForReplacement(
                model.AddressProof,
                "Address Proof");

            AddDocumentForReplacement(
                model.CancelledCheque,
                "Cancelled Cheque");

            AddDocumentForReplacement(
                model.UndertakingDocument,
                "Undertaking");

            AddDocumentForReplacement(
                model.AuthorizedPersonIdProof,
                "Authorized Person ID Proof");

            // =====================================================
            // TRANSACTION
            // =====================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Newly created physical files.
            // If DB operation fails, only these files are deleted.
            var newlySavedFiles = new List<string>();

            try
            {
                // =================================================
                // UPDATE BASIC DETAILS
                // =================================================

                application.OrganizationName =
                    model.OrganizationName;

                application.OrganizationType =
                    model.OrganizationType;

                application.RegistrationNumber =
                    model.RegistrationNumber;

                application.DateOfIncorporation =
                    model.DateOfIncorporation;

                application.PANNumber =
                    model.PANNumber.ToUpperInvariant();

                application.GSTNumber =
                    model.GSTNumber?.ToUpperInvariant();

                // =================================================
                // UPDATE ORGANIZATION PROFILE
                // =================================================

                application.SectorExperience =
                    model.SectorExperience;

                application.YearsOfExperience =
                    model.YearsOfExperience;

                application.PreviousProjects =
                    model.PreviousProjects;

                application.TrainingCentersCount =
                    model.TrainingCentersCount;

                application.TrainersAvailableCount =
                    model.TrainersAvailableCount;

                // =================================================
                // UPDATE CONTACT DETAILS
                // =================================================

                application.RegisteredOfficeAddress =
                    model.RegisteredOfficeAddress;

                application.State =
                    model.State;

                application.District =
                    model.District;

                application.City =
                    model.City;

                application.PinCode =
                    model.PinCode;

                application.Email =
                    model.Email;

                application.MobileNumber =
                    model.MobileNumber;

                application.AlternateContactPerson =
                    model.AlternateContactPerson;

                // =================================================
                // UPDATE FINANCIAL DETAILS
                // =================================================

                application.AnnualTurnoverLast3Years =
                    model.AnnualTurnoverLast3Years;

                application.BankAccountNumber =
                    model.BankAccountNumber;

                application.IFSCCode =
                    model.IFSCCode;

                application.BankName =
                    model.BankName;

                // =================================================
                // UPDATE AUTHORIZED PERSON
                // =================================================

                application.AuthorizedSignatoryName =
                    model.AuthorizedSignatoryName;

                application.Designation =
                    model.Designation;

                application.AuthorizedPersonEmail =
                    model.AuthorizedPersonEmail;

                application.AuthorizedPersonMobile =
                    model.AuthorizedPersonMobile;

                application.DeclarationAccepted =
                    model.DeclarationAccepted;

                // =================================================
                // RESET APPROVAL WORKFLOW
                // =================================================

                // =====================================================
                // RESET ALL APPROVAL WORKFLOW STAGES
                // =====================================================

                // =====================================================
                // RESET CURRENT STAGE AND ALL FOLLOWING STAGES
                // =====================================================

                switch (application.CurrentStage?.Trim().ToUpperInvariant())
                {
                    case "REGIONAL":
                        application.Regional_Head_Status = "Pending";
                        application.Regional_Head_Remarks = null;
                        application.Regional_Head_ApprovedBy = null;
                        application.Regional_Head_Date = null;

                        goto case "AFFILIATIONS";

                    case "AFFILIATIONS":

                        application.Affiliations_Status = "Pending";
                        application.Affiliations_Remarks = null;
                        application.Affiliations_ApprovedBy = null;
                        application.Affiliations_Date = null;

                        goto case "FINANCE";

                    case "FINANCE":

                        application.Finance_Status = "Pending";
                        application.Finance_Remarks = null;
                        application.Finance_ApprovedBy = null;
                        application.Finance_Date = null;

                        goto case "VERTICAL_HEAD";

                    case "VERTICAL_HEAD":

                        application.Vertical_Head_Status = "Pending";
                        application.Vertical_Head_Remarks = null;
                        application.Vertical_Head_ApprovedBy = null;
                        application.Vertical_Head_Date = null;

                        goto case "SENIOR_REVIEWER1";

                    case "SENIOR_REVIEWER1":

                        application.SeniorReviewer1_Status = "Pending";
                        application.SeniorReviewer1_Remarks = null;
                        application.SeniorReviewer1_ApprovedBy = null;
                        application.SeniorReviewer1_Date = null;

                        goto case "SENIOR_REVIEWER2";

                    case "SENIOR_REVIEWER2":

                        application.SeniorReviewer2_Status = "Pending";
                        application.SeniorReviewer2_Remarks = null;
                        application.SeniorReviewer2_ApprovedBy = null;
                        application.SeniorReviewer2_Date = null;

                        goto case "CEO";

                    case "CEO":

                        application.CEO_Status = "Pending";
                        application.CEO_Remarks = null;
                        application.CEO_ApprovedBy = null;
                        application.CEO_Date = null;

                        goto case "PAYMENT_PROCESS";

                    case "PAYMENT_PROCESS":

                        application.PaymentProcess_Status = "Pending";
                        application.PaymentProcess_Remarks = null;
                        application.PaymentProcess_ApprovedBy = null;
                        application.PaymentProcess_Date = null;

                        goto case "CERTIFICATE_ISSUANCE";

                    case "CERTIFICATE_ISSUANCE":

                        application.CertificateIssuance_Status = "Pending";
                        application.CertificateIssuance_Remarks = null;
                        application.CertificateIssuance_ApprovedBy = null;
                        application.CertificateIssuance_Date = null;

                        break;

                    default:

                        ModelState.AddModelError(
                            "",
                            $"Unknown current stage: {application.CurrentStage}");

                        ViewBag.Documents = await _context.TPRegistrationDocuments
                            .Where(x => x.TPRegistrationId == application.Id)
                            .ToListAsync();

                        return View(model);
                }

                // =====================================================
                // RESET OVERALL APPLICATION STATUS
                // =====================================================

                application.Status = "Submitted";
                application.OverallApprovalStatus = "Pending";
                application.UpdatedOn = DateTime.UtcNow;
                // =================================================
                // REPLACE DOCUMENTS
                // =================================================

                foreach (var document in documentsToReplace)
                {
                    var savedFileName = await SaveFileAsync(
                        document.File,
                        application.Id,
                        document.DocumentType);

                    var physicalPath = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "tp-registrations",
                        application.Id.ToString(),
                        savedFileName);

                    newlySavedFiles.Add(physicalPath);

                    // Remove old DB record.
                    // Physical old file is deleted only after commit.
                    if (document.ExistingDocument != null)
                    {
                        _context.TPRegistrationDocuments.Remove(
                            document.ExistingDocument);
                    }

                    _context.TPRegistrationDocuments.Add(
                        new TPRegistrationDocument
                        {
                            TPRegistrationId = application.Id,
                            DocumentType = document.DocumentType,
                            FileName = savedFileName,
                            UploadedOn = DateTime.UtcNow
                        });
                }

                // =================================================
                // SAVE DATABASE
                // =================================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // =================================================
                // DELETE OLD PHYSICAL FILES
                // =================================================

                foreach (var document in documentsToReplace)
                {
                    if (document.ExistingDocument == null)
                        continue;

                    var oldFilePath = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "tp-registrations",
                        application.Id.ToString(),
                        document.ExistingDocument.FileName);

                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch
                        {
                            // Do not fail the successful transaction
                            // because physical cleanup failed.
                        }
                    }
                }

                // =================================================
                // SUCCESS
                // =================================================

                TempData["Success"] =
                    "Application updated and resubmitted successfully.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = application.Id });
            }
            catch (Exception ex)
            {
                // =================================================
                // ROLLBACK DATABASE
                // =================================================

                await transaction.RollbackAsync();

                // =================================================
                // DELETE ONLY NEWLY UPLOADED FILES
                // =================================================

                foreach (var filePath in newlySavedFiles)
                {
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                        // Ignore cleanup failure.
                    }
                }

                // =================================================
                // RELOAD DOCUMENTS
                // =================================================

                ViewBag.Documents = await _context.TPRegistrationDocuments
                    .Where(x => x.TPRegistrationId == application.Id)
                    .ToListAsync();

                ModelState.AddModelError(
                    "",
                    $"Application could not be resubmitted: {ex.Message}");

                return View(model);
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PayNow(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var application = await _context.TPRegistrations
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.ApplicationUserId == user.Id);

            if (application == null)
                return NotFound();

            if (application.CurrentStage != "PAYMENT_PROCESS" ||
                application.OverallApprovalStatus == "Rejected" ||
                application.PaymentProcess_Status == "Approved")
            {
                TempData["ErrorMessage"] =
                    "Payment submission is not available for this application.";

                return RedirectToAction("Index", "Home");
            }

            if (application.PaymentProofStatus == "Pending")
            {
                TempData["InfoMessage"] =
                    "Your payment proof has already been submitted and is awaiting verification.";

                return RedirectToAction("Index", "Home");
            }

            var model = new PayNowVM
            {
                Id = application.Id,
                OrganizationName = application.OrganizationName,
                PaymentProofStatus = application.PaymentProofStatus
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> PayNow(PayNowVM model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Always load the application from the database.
            // Do not trust posted application status or organization details.
            var application = await _context.TPRegistrations
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    x.ApplicationUserId == user.Id);

            if (application == null)
                return NotFound();

            if (application.CurrentStage != "PAYMENT_PROCESS" ||
                application.OverallApprovalStatus == "Rejected" ||
                application.PaymentProcess_Status == "Approved")
            {
                return Forbid();
            }

            if (application.PaymentProofStatus == "Pending")
            {
                TempData["InfoMessage"] =
                    "Your payment proof is already awaiting verification.";

                return RedirectToAction("Index", "Home");
            }

            model.OrganizationName = application.OrganizationName;
            model.PaymentProofStatus = application.PaymentProofStatus;
            model.QRCodePath = "/images/tp-affiliation-payment-qr.png";

            // Validate the screenshot on the server.
            if (model.PaymentScreenshot == null ||
                model.PaymentScreenshot.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentScreenshot),
                    "Please upload your payment screenshot.");
            }
            else if (model.PaymentScreenshot.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentScreenshot),
                    "Screenshot size cannot exceed 5 MB.");
            }
            else
            {
                var extension = Path.GetExtension(
                    model.PaymentScreenshot.FileName).ToLowerInvariant();

                if (extension != ".jpg" &&
                    extension != ".jpeg" &&
                    extension != ".png")
                {
                    ModelState.AddModelError(
                        nameof(model.PaymentScreenshot),
                        "Only JPG, JPEG and PNG screenshots are allowed.");
                }
                else if (!await IsValidPaymentImageAsync(model.PaymentScreenshot))
                {
                    ModelState.AddModelError(
                        nameof(model.PaymentScreenshot),
                        "The uploaded file is not a valid JPG or PNG image.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            // Store proof OUTSIDE wwwroot so it cannot be downloaded
            // directly through a public URL.
            // Store payment proof inside wwwroot.
            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "TPPaymentProofs");

            Directory.CreateDirectory(uploadFolder);



            var extensionToSave =
                Path.GetExtension(model.PaymentScreenshot!.FileName)
                    .ToLowerInvariant();

            var safeFileName = $"{Guid.NewGuid():N}{extensionToSave}";
            var fullPath = Path.Combine(uploadFolder, safeFileName);

            try
            {
                await using (var stream = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await model.PaymentScreenshot.CopyToAsync(stream);
                }

                // Store only the generated filename, never the client filename/path.
                application.PaymentTransactionId =
                    model.PaymentTransactionId.Trim();

                application.PaymentProofFileName = safeFileName;
                application.PaymentProofStatus = "Pending";
                application.PaymentProofSubmittedOn = DateTime.UtcNow;

                // IMPORTANT:
                // Do NOT set PaymentProcess_Status = "Approved".
                // Do NOT advance CurrentStage here.
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Avoid leaving an orphaned upload if saving the database fails.
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                throw;
            }

            TempData["SuccessMessage"] =
                "Payment proof submitted successfully. Finance will verify your payment.";

            return RedirectToAction("Index", "Home");
        }

        private static async Task<bool> IsValidPaymentImageAsync(IFormFile file)
        {
            // Verify file signatures, not just the supplied extension.
            var header = new byte[8];

            await using var stream = file.OpenReadStream();

            var bytesRead = await stream.ReadAsync(
                header.AsMemory(0, header.Length));

            // JPEG starts FF D8 FF
            var isJpeg = bytesRead >= 3 &&
                         header[0] == 0xFF &&
                         header[1] == 0xD8 &&
                         header[2] == 0xFF;

            // PNG starts 89 50 4E 47 0D 0A 1A 0A
            var isPng = bytesRead >= 8 &&
                        header[0] == 0x89 &&
                        header[1] == 0x50 &&
                        header[2] == 0x4E &&
                        header[3] == 0x47 &&
                        header[4] == 0x0D &&
                        header[5] == 0x0A &&
                        header[6] == 0x1A &&
                        header[7] == 0x0A;

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            return (isJpeg && (extension == ".jpg" || extension == ".jpeg"))
                || (isPng && extension == ".png");
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DownloadCertificate(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            // Get only the registration belonging to the logged-in TP.
            var registration = await _context.TPRegistrations
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.ApplicationUserId == user.Id);

            if (registration == null)
                return NotFound("TP registration not found.");

            // Certificate must be approved before download.
            if (!string.Equals(
                    registration.CertificateIssuance_Status,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            // Configure this path to match your actual certificate template.
            var templatePath = Path.Combine(
             _environment.WebRootPath,
             "uploads",
             "certificates",
             "templates",
             "TP_certificate.png");

            if (!System.IO.File.Exists(templatePath))
                return NotFound("Certificate template file was not found.");

            var certificateNo = $"TP-{registration.Id:D5}";
            var companyName = registration.OrganizationName ?? "Training Partner";
            var membershipType = registration.MembershipType ?? "Certificate";

            var description =
                "is hereby recognized as an affiliated Training Partner.";

            var issuanceDate =
                registration.CertificateIssuance_Date ?? DateTime.UtcNow;

            var startDate = issuanceDate.ToString("dd MMM yyyy");
            var endDate = issuanceDate.AddYears(1).ToString("dd MMM yyyy");

            var pdfBytes = GenerateCertificatePdf(
                membershipType,
                certificateNo,
                companyName,
                description,
                startDate,
                endDate,
                templatePath);

            var fileName = $"TP-Certificate-{registration.Id}.pdf";

            return File(
                pdfBytes,
                "application/pdf",
                fileName);
        }
        //    private byte[] GenerateCertificatePdf(
        //        string certificateNo,
        //        string companyName,
        //        string description,
        //        string startDate,
        //        string endDate,
        //        string templatePath)
        //    {
        //        // =====================================================
        //        // CREATE PDF MEMORY
        //        // =====================================================

        //        using (MemoryStream outputStream =
        //               new MemoryStream())
        //        {
        //            // =================================================
        //            // A4 LANDSCAPE
        //            // =================================================

        //            iText.Kernel.Geom.PageSize pageSize =
        //                iText.Kernel.Geom.PageSize
        //                    .A4
        //                    .Rotate();


        //            // =================================================
        //            // PDF WRITER
        //            // =================================================

        //            PdfWriter writer =
        //                new PdfWriter(
        //                    outputStream
        //                );


        //            // =================================================
        //            // PDF DOCUMENT
        //            // =================================================

        //            PdfDocument pdf =
        //                new PdfDocument(
        //                    writer
        //                );


        //            pdf.SetDefaultPageSize(
        //                pageSize
        //            );


        //            // =================================================
        //            // LAYOUT DOCUMENT
        //            // =================================================

        //            iText.Layout.Document document =
        //                new iText.Layout.Document(
        //                    pdf
        //                );


        //            document.SetMargins(
        //                0,
        //                0,
        //                0,
        //                0
        //            );


        //            float pageWidth =
        //                pageSize.GetWidth();


        //            float pageHeight =
        //                pageSize.GetHeight();


        //            // =================================================
        //            // FONTS
        //            // =================================================
        //            PdfFont regularFont =
        //                PdfFontFactory.CreateFont(
        //                    StandardFonts.HELVETICA
        //                );

        //            PdfFont boldFont =
        //                PdfFontFactory.CreateFont(
        //                    StandardFonts.HELVETICA_BOLD
        //                );

        //            PdfFont timesFont =
        //                PdfFontFactory.CreateFont(
        //                    StandardFonts.TIMES_ROMAN
        //                );
        //            // =================================================
        //            // BACKGROUND IMAGE
        //            // =================================================

        //            ImageData imageData =
        //                ImageDataFactory.Create(
        //                    templatePath
        //                );


        //            Image background =
        //                new Image(
        //                    imageData
        //                );


        //            background
        //                .ScaleAbsolute(
        //                    pageWidth,
        //                    pageHeight
        //                )
        //                .SetFixedPosition(
        //                    0,
        //                    0
        //                );


        //            document.Add(
        //                background
        //            );


        //            // =================================================
        //            // COMPANY NAME
        //            // =================================================
        //            Paragraph company =
        // new Paragraph(companyName)
        //     .SetFont(boldFont)
        //     .SetFontSize(18)
        //     .SetFontColor(ColorConstants.BLACK)
        //     .SetTextAlignment(
        //         iText.Layout.Properties.TextAlignment.CENTER
        //     )
        //     .SetFixedPosition(
        //         0,
        //         290,
        //         pageWidth
        //     );

        //            document.Add(company);

        //            // =================================================
        //            // DESCRIPTION
        //            // =================================================

        //            Paragraph descriptionText =
        // new Paragraph(description)
        //     .SetFont(timesFont)
        //     .SetFontSize(18)
        //     .SetFontColor(ColorConstants.BLACK)
        //     .SetTextAlignment(
        //         iText.Layout.Properties.TextAlignment.CENTER
        //     )
        //     .SetFixedPosition(
        //         0,
        //         252,
        //         pageWidth
        //     );

        //            document.Add(descriptionText);
        //            // =================================================
        //            // DATE
        //            // =================================================

        //            Paragraph dates =
        //new Paragraph()
        //    .SetTextAlignment(
        //        iText.Layout.Properties.TextAlignment.CENTER
        //    )
        //    .SetFixedPosition(
        //        0,
        //        215,
        //        pageWidth
        //    );

        //            dates.Add(
        //                new Text(startDate)
        //                    .SetFont(boldFont)
        //                    .SetFontSize(20)
        //                    .SetFontColor(ColorConstants.BLACK)
        //            );

        //            dates.Add(
        //                new Text("  to  ")
        //                    .SetFont(regularFont)
        //                    .SetFontSize(20)
        //                    .SetFontColor(ColorConstants.BLACK)
        //            );

        //            dates.Add(
        //                new Text(endDate)
        //                    .SetFont(boldFont)
        //                    .SetFontSize(20)
        //                    .SetFontColor(ColorConstants.BLACK)
        //            );

        //            document.Add(dates);
        //            // =================================================
        //            // ID NUMBER
        //            // CertificateNo = ID No.
        //            // =================================================

        //            Paragraph idNumber =
        //                new Paragraph()
        //                    .SetTextAlignment(
        //                        iText.Layout.Properties.TextAlignment.LEFT
        //                    )
        //                    .SetFixedPosition(
        //                        88,
        //                        75,
        //                        220
        //                    );

        //            idNumber.Add(
        //                new Text("ID No.: ")
        //                    .SetFont(regularFont)
        //                    .SetFontSize(10)
        //                    .SetFontColor(
        //                        ColorConstants.BLACK
        //                    )
        //            );

        //            idNumber.Add(
        //                new Text(certificateNo)
        //                    .SetFont(boldFont)
        //                    .SetFontSize(10)
        //                    .SetFontColor(
        //                        ColorConstants.BLACK
        //                    )
        //            );

        //            document.Add(idNumber);
        //            // =================================================
        //            // CLOSE
        //            // =================================================

        //            document.Close();


        //            // =================================================
        //            // RETURN PDF
        //            // =================================================

        //            return outputStream.ToArray();
        //        }
        //    }

        private byte[] GenerateCertificatePdf(
        string membershipType,
        string certificateNo,
        string companyName,
        string description,
        string startDate,
        string endDate,
        string templatePath)
        {
            // =====================================================
            // CREATE PDF MEMORY
            // =====================================================

            using (MemoryStream outputStream = new MemoryStream())
            {
                // =================================================
                // A4 LANDSCAPE
                // =================================================

                iText.Kernel.Geom.PageSize pageSize =
                    iText.Kernel.Geom.PageSize.A4.Rotate();

                // =================================================
                // PDF WRITER
                // =================================================

                PdfWriter writer = new PdfWriter(outputStream);

                // =================================================
                // PDF DOCUMENT
                // =================================================

                PdfDocument pdf = new PdfDocument(writer);

                pdf.SetDefaultPageSize(pageSize);

                // =================================================
                // LAYOUT DOCUMENT
                // =================================================

                iText.Layout.Document document =
                    new iText.Layout.Document(pdf);

                document.SetMargins(0, 0, 0, 0);

                float pageWidth = pageSize.GetWidth();
                float pageHeight = pageSize.GetHeight();

                // =================================================
                // FONTS
                // =================================================

                PdfFont regularFont =
                    PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                PdfFont boldFont =
                    PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                PdfFont timesFont =
                    PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);


                string mariageFontPath = Path.Combine(
                    _environment.WebRootPath,
                    "Fonts",
                    "Old English Text MT Regular.ttf"
                );


                if (!System.IO.File.Exists(mariageFontPath))
                {
                    throw new FileNotFoundException(
                        "Mariage font file not found.",
                        mariageFontPath
                    );
                }

                PdfFont mariageFont = PdfFontFactory.CreateFont(
                    mariageFontPath,
                    iText.IO.Font.PdfEncodings.WINANSI,
                    PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED
                );
                // =================================================
                // BACKGROUND IMAGE
                // =================================================

                ImageData imageData = ImageDataFactory.Create(templatePath);

                Image background = new Image(imageData);

                background
                    .ScaleAbsolute(pageWidth, pageHeight)
                    .SetFixedPosition(0, 0);

                document.Add(background);

                // =================================================
                // MEMBERSHIP TYPE HEADING (NEW)
                // =================================================

                //Paragraph membershipHeading =
                //    new Paragraph(membershipType)
                //        .SetFont(boldFont)
                //        .SetFontSize(22)
                //        .SetFontColor(ColorConstants.BLACK)
                //        .SetTextAlignment(
                //            iText.Layout.Properties.TextAlignment.CENTER
                //        )
                //        .SetFixedPosition(
                //            0,
                //            330,
                //            pageWidth
                //        );
                Paragraph membershipHeading =
     new Paragraph(membershipType)
         .SetFont(mariageFont)
         .SetFontSize(35)
         .SetFontColor(new iText.Kernel.Colors.DeviceRgb(20, 74, 194))   // heading blue #1A46A4
         .SetUnderline(2f, -4f)
         .SetTextAlignment(
             iText.Layout.Properties.TextAlignment.CENTER
         )
         .SetFixedPosition(
             0,
             343,
             pageWidth
         );

                document.Add(membershipHeading);

                document.Add(membershipHeading);
                document.Add(membershipHeading);

                // =================================================
                // COMPANY NAME
                // =================================================

                Paragraph company =
                    new Paragraph(companyName)
                        .SetFont(boldFont)
                        .SetFontSize(18)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.CENTER
                        )
                        .SetFixedPosition(0, 290, pageWidth);

                document.Add(company);

                // =================================================
                // DESCRIPTION
                // =================================================

                Paragraph descriptionText =
                    new Paragraph(description)
                        .SetFont(timesFont)
                        .SetFontSize(18)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.CENTER
                        )
                        .SetFixedPosition(0, 252, pageWidth);

                document.Add(descriptionText);

                // =================================================
                // DATE
                // =================================================

                Paragraph dates =
                    new Paragraph()
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.CENTER
                        )
                        .SetFixedPosition(0, 215, pageWidth);

                dates.Add(
                    new Text(startDate)
                        .SetFont(boldFont)
                        .SetFontSize(20)
                        .SetFontColor(ColorConstants.BLACK)
                );

                dates.Add(
                    new Text("  to  ")
                        .SetFont(regularFont)
                        .SetFontSize(20)
                        .SetFontColor(ColorConstants.BLACK)
                );

                dates.Add(
                    new Text(endDate)
                        .SetFont(boldFont)
                        .SetFontSize(20)
                        .SetFontColor(ColorConstants.BLACK)
                );

                document.Add(dates);

                // =================================================
                // ID NUMBER
                // CertificateNo = ID No.
                // =================================================

                Paragraph idNumber =
                    new Paragraph()
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.LEFT
                        )
                        .SetFixedPosition(88, 75, 220);

                idNumber.Add(
                    new Text("ID No.: ")
                        .SetFont(regularFont)
                        .SetFontSize(10)
                        .SetFontColor(ColorConstants.BLACK)
                );

                idNumber.Add(
                    new Text(certificateNo)
                        .SetFont(boldFont)
                        .SetFontSize(10)
                        .SetFontColor(ColorConstants.BLACK)
                );

                document.Add(idNumber);

                // =================================================
                // CLOSE
                // =================================================

                document.Close();

                // =================================================
                // RETURN PDF
                // =================================================

                return outputStream.ToArray();
            }
        }
    }
}
