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

namespace TSSC.Unified.Areas.TP.Controllers
{
    [Area("TP")]
    public class RegistrationController : Controller
    {
        private readonly AppdbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;

        public RegistrationController(
        AppdbContext context,
        IWebHostEnvironment environment,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager, EmailService emailService)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View(new TPRegistrationVM());
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        public async Task<IActionResult> Create(TPRegistrationVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

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

            foreach (var uploadedFile in uploadedFiles)
            {
                if (uploadedFile.File == null || uploadedFile.File.Length == 0)
                    continue;

                var extension = Path.GetExtension(uploadedFile.File.FileName);

                if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) &&
                    uploadedFile.File.Length > maxPdfSize)
                {
                    ModelState.AddModelError(
                        uploadedFile.FieldName,
                        $"{uploadedFile.Label} PDF must not exceed 2 MB.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            if (!await _roleManager.RoleExistsAsync("TrainingPartner"))
            {
                ModelState.AddModelError("", "TrainingPartner role is not configured.");
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email),
                    "An account already exists with this email address.");
                return View(model);
            }

            TPRegistration? application = null;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                application = new TPRegistration
                {
                    OrganizationName = model.OrganizationName,
                    OrganizationType = model.OrganizationType,
                    RegistrationNumber = model.RegistrationNumber,
                    DateOfIncorporation = model.DateOfIncorporation,
                    PANNumber = model.PANNumber.ToUpperInvariant(),
                    GSTNumber = model.GSTNumber?.ToUpperInvariant(),

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
                    CreatedOn = DateTime.UtcNow
                };

                _context.TPRegistrations.Add(application);
                await _context.SaveChangesAsync();

                var documents = new List<(IFormFile? File, string DocumentType)>
        {
            (model.FinancialDocuments, "Financial Documents"),
            (model.RegistrationCertificate, "Registration Certificate"),
            (model.PANCardCopy, "PAN Card Copy"),
            (model.GSTCertificate, "GST Certificate"),
            (model.AddressProof, "Address Proof"),
            (model.CancelledCheque, "Cancelled Cheque"),
            (model.UndertakingDocument, "Undertaking"),
            (model.AuthorizedPersonIdProof, "Authorized Person ID Proof")
        };

                foreach (var document in documents.Where(x => x.File != null))
                {
                    var savedFileName = await SaveFileAsync(
                        document.File!,
                        application.Id,
                        document.DocumentType);

                    _context.TPRegistrationDocuments.Add(new TPRegistrationDocument
                    {
                        TPRegistrationId = application.Id,
                        DocumentType = document.DocumentType,
                        FileName = savedFileName,
                        UploadedOn = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                // Generate a secure temporary password, then send a password-set/reset link.
                var temporaryPassword = GenerateTemporaryPassword();
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.MobileNumber,
                    EmailConfirmed = false,
                    Password=temporaryPassword
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


                // Send only after registration, documents, account, and role are committed.
                try
                {
                    var templatePath = Path.Combine(
                        _environment.WebRootPath,
                        "email-templates",
                        "tp_registration-submitted.html");

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
                    // Do not roll back the successfully committed registration because SMTP failed.
                    //_logger.LogError(
                    //    emailException,
                    //    "TP Registration {ApplicationId} completed, but the acknowledgement email failed.",
                    //    application.Id);
                }

                TempData["Success"] =
                    "TP registration submitted successfully. Your account has been created.";

                return RedirectToAction(nameof(Confirmation));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Uploaded files are outside the database transaction, so remove them manually.
                if (application != null)
                {
                    var uploadFolder = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "tp-registrations",
                        application.Id.ToString());

                    if (Directory.Exists(uploadFolder))
                        Directory.Delete(uploadFolder, recursive: true);
                }

                ModelState.AddModelError("", $"Registration could not be submitted: {ex.Message}");
                return View(model);
            }
        }

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
            var digits = new char[8];

            for (var i = 0; i < digits.Length; i++)
            {
                digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            }

            return new string(digits);
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
                application.Vertical_Head_Status == "Rejected" ||
                application.Finance_Status == "Rejected" ||
                application.CEO_Status == "Rejected";

            if (!canEdit)
                return Forbid();

            var model = new TPRegistrationEditVM
            {
                Id = application.Id,

                OrganizationName = application.OrganizationName,
                OrganizationType = application.OrganizationType,
                RegistrationNumber = application.RegistrationNumber,
                DateOfIncorporation = application.DateOfIncorporation,
                PANNumber = application.PANNumber,
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
                application.Vertical_Head_Status == "Rejected" ||
                application.Finance_Status == "Rejected" ||
                application.CEO_Status == "Rejected";

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

                application.Regional_Head_Status = "Pending";
                application.Regional_Head_Remarks = null;
                application.Regional_Head_ApprovedBy = null;
                application.Regional_Head_Date = null;

                application.Vertical_Head_Status = "Pending";
                application.Vertical_Head_Remarks = null;
                application.Vertical_Head_ApprovedBy = null;
                application.Vertical_Head_Date = null;

                application.Finance_Status = "Pending";
                application.Finance_Remarks = null;
                application.Finance_ApprovedBy = null;
                application.Finance_Date = null;

                application.CEO_Status = "Pending";
                application.CEO_Remarks = null;
                application.CEO_ApprovedBy = null;
                application.CEO_Date = null;

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
    }
}
