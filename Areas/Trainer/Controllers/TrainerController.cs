using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;
using TSSC.Unified.Areas.Admin.Controllers;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.Trainer.Controllers
{
    [Area("Trainer")]
    public class TrainerController :Controller
    {
        private readonly ILogger<TrainerController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        private readonly EmailService _emailService;
        public TrainerController(
            ILogger<TrainerController> logger,
            AppdbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment,
            EmailService emailService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
            _emailService = emailService;
        }
        private string GenerateRandomPassword(int length = 10)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string numbers = "23456789";
            const string special = "@#$!";

            var random = new Random();

            var password =
                upper[random.Next(upper.Length)].ToString() +
                lower[random.Next(lower.Length)] +
                numbers[random.Next(numbers.Length)] +
                special[random.Next(special.Length)];

            const string all = upper + lower + numbers + special;

            for (int i = password.Length; i < length; i++)
            {
                password += all[random.Next(all.Length)];
            }

            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetLocationByPincode(string pincode)
        {
            if (string.IsNullOrWhiteSpace(pincode))
            {
                return Json(new
                {
                    success = false,
                    message = "Pincode is required."
                });
            }

            pincode = pincode.Trim();

            if (!System.Text.RegularExpressions.Regex.IsMatch(pincode, @"^\d{6}$"))
            {
                return Json(new
                {
                    success = false,
                    message = "Please enter a valid 6 digit pincode."
                });
            }

            var locations = (
                from city in _context.City
                join district in _context.District
                    on city.DistrictId equals district.Id
                join state in _context.State
                    on district.StateId equals state.Id
                where city.Pincode == pincode
                      && city.IsActive
                      && district.IsActive
                select new
                {
                    cityId = city.Id,
                    cityName = city.CityName,
                    districtId = district.Id,
                    districtName = district.DistrictName,
                    stateId = state.Id,
                    stateName = state.StateName,
                    pincode = city.Pincode
                }
            )
            .OrderBy(x => x.cityName)
            .ToList();

            if (!locations.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "No location found for this pincode."
                });
            }

            return Json(new
            {
                success = true,
                locations = locations
            });
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetDistricts(int stateId)
        {
            var districts = _context.District
                .Where(x => x.StateId == stateId && x.IsActive)
                .OrderBy(x => x.DistrictName)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.DistrictName
                })
                .ToList();

            return Json(districts);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetCities(int districtId)
        {
            var cities = _context.City
                .Where(x => x.DistrictId == districtId && x.IsActive)
                .OrderBy(x => x.CityName)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.CityName,
                    pincode = x.Pincode
                })
                .ToList();

            return Json(cities);
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Registration()
        {
            var model = new TrainerRegistrationViewModel();
            model.JobRoleList = _context.JobRoles
            .Where(x => x.IsActive)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.JobRoleTitle
            })
            .ToList();

            // State
            model.StateList = _context.State
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.StateName
                })
                .ToList();
            // First row by default
            model.Qualifications.Add(new TrainerQualificationVM());
            model.Experiences.Add(new TrainerExperienceVM());

            return View(model);
        }
        private void LoadDropdowns(TrainerRegistrationViewModel model)
        {
            // Job Role
            model.JobRoleList = _context.JobRoles
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.JobRoleTitle
                })
                .ToList();

            // State
            model.StateList = _context.State
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.StateName
                })
                .ToList();

            // District
            model.DistrictList = new List<SelectListItem>();

            if (model.StateId.HasValue)
            {
                model.DistrictList = _context.District
                    .Where(x =>
                        x.StateId == model.StateId.Value &&
                        x.IsActive)
                    .OrderBy(x => x.DistrictName)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.DistrictName
                    })
                    .ToList();
            }

            // City
            model.CityList = new List<SelectListItem>();

            if (model.DistrictId.HasValue)
            {
                model.CityList = _context.City
                    .Where(x =>
                        x.DistrictId == model.DistrictId.Value &&
                        x.IsActive)
                    .OrderBy(x => x.CityName)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.CityName
                    })
                    .ToList();
            }
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration(TrainerRegistrationViewModel model)
        {
            // =====================================================
            // 1. VALIDATE EMAIL
            // =====================================================

            var normalizedEmail = model.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email is required."
                );
            }
            else
            {
                var emailExists = await _context.TrainerRegistration
                    .AnyAsync(x =>
                        x.Email != null &&
                        x.Email.Trim().ToLower() == normalizedEmail);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "This email is already registered."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
                return View(model);
            }


            // =====================================================
            // 2. PREPARE LOGIN CREDENTIALS
            // =====================================================

            var email = normalizedEmail!;

            var generatedPassword = GenerateRandomPassword();


            // =====================================================
            // 3. START TRANSACTION
            // =====================================================

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            var uploadedFiles = new List<string>();

            try
            {
                // =====================================================
                // 4. CREATE TRAINER LOGIN ACCOUNT
                // =====================================================

             var user = new AppUser
{
    UserName = email,
    Email = email,
    Name = model.CandidateName,
    MobileNo = model.Mobile,
    UserRole = "Trainer",
    EmailConfirmed = true,
    CreatedOn = DateTime.Now,
    Password= generatedPassword
             };

                var userResult = await _userManager.CreateAsync(
                    user,
                    generatedPassword
                );

                if (!userResult.Succeeded)
                {
                    foreach (var error in userResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description
                        );
                    }

                    await transaction.RollbackAsync();

                    LoadDropdowns(model);
                    return View(model);
                }


                // =====================================================
                // 5. TRAINER ROLE
                // =====================================================

                if (!await _roleManager.RoleExistsAsync("Trainer"))
                {
                    var createRoleResult =
                        await _roleManager.CreateAsync(
                            new IdentityRole("Trainer")
                        );

                    if (!createRoleResult.Succeeded)
                    {
                        foreach (var error in createRoleResult.Errors)
                        {
                            ModelState.AddModelError(
                                string.Empty,
                                error.Description
                            );
                        }

                        await transaction.RollbackAsync();

                        LoadDropdowns(model);
                        return View(model);
                    }
                }

                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        "Trainer"
                    );

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description
                        );
                    }

                    await transaction.RollbackAsync();

                    LoadDropdowns(model);
                    return View(model);
                }


                // =====================================================
                // 6. MAIN REGISTRATION
                // =====================================================

                var registration = new TrainerRegistration
                {
                    RegistrationMode = "Online",

                    CandidateName = model.CandidateName,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,

                    JobRoleId = model.JobRoleId,
                    TPId = model.TPId,
                    SPOCName = model.SPOCName,
                    SIDHApplied = model.SIDHApplied,
                    Scheme = model.Scheme,

                    Address = model.Address,
                    StateId = model.StateId,
                    DistrictId = model.DistrictId,
                    CityId = model.CityId,
                    Pincode = model.Pincode,

                    Email = email,
                    Mobile = model.Mobile,
                    MobileVerified = model.MobileVerified,

                    ReferenceName = model.ReferenceName,
                    ReferenceDesignation = model.ReferenceDesignation,
                    ReferenceOrganization = model.ReferenceOrganization,
                    ReferenceEmail = model.ReferenceEmail,
                    ReferenceMobile = model.ReferenceMobile,

                    TermsAccepted = model.TermsAccepted,
                    AuthenticityAccepted = model.AuthenticityAccepted,

                    DeclarationDate = DateTime.Now,
                    Status = "Pending",
                    CreatedDate = DateTime.Now,

                    EmailVerified = true
                };

                _context.TrainerRegistration.Add(registration);

                await _context.SaveChangesAsync();


                // =====================================================
                // 7. REGISTRATION NUMBER
                // =====================================================

                registration.RegistrationNo =
                    $"TRN-{DateTime.Now:yyyy}-{registration.Id:D6}";

                await _context.SaveChangesAsync();


                // =====================================================
                // 3. CREATE UPLOAD FOLDER
                // =====================================================

                string uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "trainer-registration",
                    registration.Id.ToString()
                );

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }


                // =====================================================
                // 4. AADHAAR
                // =====================================================

                if (model.AadhaarFile != null &&
                    model.AadhaarFile.Length > 0)
                {
                    string filePath = await SavePdfFile(
                        model.AadhaarFile,
                        uploadFolder,
                        uploadedFiles
                    );

                    _context.TrainerRegistrationDocument.Add(
                        new TrainerRegistrationDocument
                        {
                            TrainerRegistrationId = registration.Id,

                            DocumentType = "Aadhaar",

                            FileName = Path.GetFileName(
                                model.AadhaarFile.FileName
                            ),

                            FilePath = filePath,

                            ContentType =
                                model.AadhaarFile.ContentType,

                            FileSize =
                                model.AadhaarFile.Length,

                            VerificationStatus = "Pending",

                            CreatedDate = DateTime.Now
                        }
                    );
                }


                // =====================================================
                // 5. PAN CARD
                // =====================================================

                if (model.PanCardFile != null &&
                    model.PanCardFile.Length > 0)
                {
                    string filePath = await SavePdfFile(
                        model.PanCardFile,
                        uploadFolder,
                        uploadedFiles
                    );

                    _context.TrainerRegistrationDocument.Add(
                        new TrainerRegistrationDocument
                        {
                            TrainerRegistrationId = registration.Id,

                            DocumentType = "PAN",

                            FileName = Path.GetFileName(
                                model.PanCardFile.FileName
                            ),

                            FilePath = filePath,

                            ContentType =
                                model.PanCardFile.ContentType,

                            FileSize =
                                model.PanCardFile.Length,

                            VerificationStatus = "Pending",

                            CreatedDate = DateTime.Now
                        }
                    );
                }


                // =====================================================
                // 6. QUALIFICATIONS
                // =====================================================

                if (model.Qualifications != null)
                {
                    foreach (var item in model.Qualifications)
                    {
                        // Ignore completely empty row
                        if (string.IsNullOrWhiteSpace(item.Qualification) &&
                            string.IsNullOrWhiteSpace(item.Board) &&
                            string.IsNullOrWhiteSpace(item.PassingYear) &&
                            string.IsNullOrWhiteSpace(item.PercentageOrCGPA) &&
                            item.File == null)
                        {
                            continue;
                        }

                        string? fileName = null;
                        string? filePath = null;


                        // Qualification PDF
                        if (item.File != null &&
                            item.File.Length > 0)
                        {
                            filePath = await SavePdfFile(
                                item.File,
                                uploadFolder,
                                uploadedFiles
                            );

                            fileName = Path.GetFileName(
                                item.File.FileName
                            );
                        }


                        var qualification =
                            new TrainerRegistrationQualification
                            {
                                TrainerRegistrationId =
                                    registration.Id,

                                Qualification =
                                    item.Qualification,

                                Board =
                                    item.Board,

                                PassingYear =
                                    item.PassingYear,

                                PercentageOrCGPA =
                                    item.PercentageOrCGPA,

                                FileName =
                                    fileName,

                                FilePath =
                                    filePath,

                                CreatedDate =
                                    DateTime.Now
                            };

                        _context.TrainerRegistrationQualification
                            .Add(qualification);
                    }
                }


                // =====================================================
                // 7. EXPERIENCES
                // =====================================================

                if (model.Experiences != null)
                {
                    foreach (var item in model.Experiences)
                    {
                        // Ignore completely empty row
                        if (string.IsNullOrWhiteSpace(item.OrganizationName) &&
                            string.IsNullOrWhiteSpace(item.Designation) &&
                            item.FromDate == null &&
                            item.ToDate == null &&
                            string.IsNullOrWhiteSpace(item.Duration) &&
                            item.File == null)
                        {
                            continue;
                        }

                        string? fileName = null;
                        string? filePath = null;


                        // Experience PDF
                        if (item.File != null &&
                            item.File.Length > 0)
                        {
                            filePath = await SavePdfFile(
                                item.File,
                                uploadFolder,
                                uploadedFiles
                            );

                            fileName = Path.GetFileName(
                                item.File.FileName
                            );
                        }


                        var experience =
                            new TrainerRegistrationExperience
                            {
                                TrainerRegistrationId = registration.Id,

                                OrganizationName = item.OrganizationName,

                                Designation = item.Designation,

                                FromDate =item.FromDate,

                                ToDate = item.ToDate,

                                Duration = item.Duration,

                                FileName = fileName,

                                FilePath = filePath,

                                CreatedDate = DateTime.Now
                            };

                        _context.TrainerRegistrationExperience
                            .Add(experience);
                    }
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                //Email 1  --------------------------------------------------------------------
                try
                {
                    string templatePath = Path.Combine(
                        _environment.WebRootPath,
                        "EmailTemplates",
                        "TrainerRegistrationSubmitted.html"
                    );

                    string baseUrl = $"{Request.Scheme}://{Request.Host}";

                    var replacements = new Dictionary<string, string>
    {
        { "BaseUrl", baseUrl },
        { "Name", model.CandidateName }
    };

                    string result = await _emailService.SendEmailAsync(
                        email,
                        "Trainer Registration Successfully Submitted - TSSC",
                        templatePath,
                        replacements
                    );

                    if (result != "True")
                    {
                        _logger.LogError(
                            "Trainer registration confirmation email failed for {Email}. Error: {Error}",
                            email,
                            result
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unable to send trainer registration confirmation email to {Email}",
                        email
                    );
                }
                //Email 2  --------------------------------------------------------------------
                try
                {
                    string templatePath = Path.Combine(
                        _environment.WebRootPath,
                        "EmailTemplates",
                        "TrainerCredentials.html"
                    );

                    string baseUrl = $"{Request.Scheme}://{Request.Host}";

                    var replacements = new Dictionary<string, string>
                {
                    { "BaseUrl", baseUrl },
                    { "Name", model.CandidateName },
                    { "Username", email },
                    { "Password", generatedPassword }
                };

                    string result = await _emailService.SendEmailAsync(
                        email,
                        "Welcome to TSSC - Trainer Account Created",
                        templatePath,
                        replacements
                    );

                    if (result != "True")
                    {
                        _logger.LogError(
                            "Trainer credentials email failed for {Email}. Error: {Error}",
                            email,
                            result
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unable to send trainer credentials email to {Email}",
                        email
                    );
                }

                // =====================================================
                // 10. SUCCESS
                // =====================================================

                TempData["msg"] =
                    $"Registration submitted successfully. " +
                    $"Registration No: {registration.RegistrationNo}";

                return RedirectToAction(nameof(Registration));
            }
            catch (Exception ex)
            {
                // Rollback database transaction
                await transaction.RollbackAsync();


                // Delete already uploaded files
                foreach (var file in uploadedFiles)
                {
                    try
                    {
                        if (System.IO.File.Exists(file))
                        {
                            System.IO.File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup error
                    }
                }


                _logger.LogError(
                    ex,
                    "Trainer registration failed."
                );


                ModelState.AddModelError(
                    "",
                    "Something went wrong while submitting the registration."
                );


                LoadDropdowns(model);

                return View(model);
            }
        }
        private async Task<string> SavePdfFile( IFormFile file,string uploadFolder,List<string> uploadedFiles)
        {
            // =====================================================
            // MAX FILE SIZE = 5 MB
            // =====================================================

            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                throw new Exception(
                    $"File '{file.FileName}' cannot be larger than 5 MB."
                );
            }


            // =====================================================
            // ONLY PDF
            // =====================================================

            string extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (extension != ".pdf")
            {
                throw new Exception(
                    $"Only PDF files are allowed. " +
                    $"Invalid file: {file.FileName}"
                );
            }


            // =====================================================
            // UNIQUE FILE NAME
            // =====================================================

            string uniqueFileName =
                $"{Guid.NewGuid():N}.pdf";


            // =====================================================
            // PHYSICAL PATH
            // =====================================================

            string physicalPath =
                Path.Combine(
                    uploadFolder,
                    uniqueFileName
                );


            // =====================================================
            // SAVE FILE
            // =====================================================

            await using (var stream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await file.CopyToAsync(stream);
            }


            // Keep path for rollback
            uploadedFiles.Add(physicalPath);


            // =====================================================
            // RETURN WEB PATH
            // =====================================================

            return
                $"/uploads/trainer-registration/" +
                $"{Path.GetFileName(uploadFolder)}/" +
                $"{uniqueFileName}";
        }
        //public async Task<IActionResult> TestRegistrationEmail(int id = 10)
        //{
        //    var application = await _context.TrainerRegistration
        //        .FirstOrDefaultAsync(x => x.Id == id);

        //    if (application == null)
        //        return NotFound("Trainer registration was not found.");

        //    AppUser? user = null;

        //    if (!string.IsNullOrWhiteSpace(application.Email))
        //    {
        //        user = await _userManager.FindByEmailAsync(application.Email);
        //    }

        //    var templatePath1 = Path.Combine(
        //        _environment.WebRootPath,
        //        "EmailTemplates",
        //        "TrainerRegistrationSubmitted.html"
        //    );

        //    var result1 = await _emailService.SendEmailAsync(
        //        application.Email,
        //        "Trainer Registration Submitted Successfully",
        //        templatePath1,
        //        new Dictionary<string, string>
        //        {
        //            ["Name"] = application.CandidateName
        //        });

        //    var templatePath2 = Path.Combine(
        //        _environment.WebRootPath,
        //        "EmailTemplates",
        //        "TrainerCredentials.html"
        //    );

        //    var result2 = await _emailService.SendEmailAsync(
        //        application.Email,
        //        "Trainer Registration Login Credentials",
        //        templatePath2,
        //        new Dictionary<string, string>
        //        {
        //            ["Name"] = application.CandidateName,
        //            ["Username"] = user?.Email ?? application.Email,
        //            ["Password"] = "Test@12345"
        //        });

        //    return Ok(new
        //    {
        //        Email = application.Email,
        //        Email1 = result1,
        //        Email2 = result2
        //    });
        //}

       
    }
}
