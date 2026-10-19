using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.TP.Controllers
{
    [Area("TP")]
    public class TrainingCenter :Controller
    {
        private readonly AppdbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;

        public TrainingCenter(AppdbContext context,
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
        public async Task<IActionResult> SelectJobRole()
        {
            var trainingCenterModel = new TrainingCenterViewModel();
            await LoadSchemes(trainingCenterModel);

            ViewBag.Schemes = trainingCenterModel.Scheme;

            var jobRoles = await _context.JobRoles
                .AsNoTracking()
                .Where(x => x.Status == "Approved"
                         && x.IsActive
                         && (!x.ValidFrom.HasValue || x.ValidFrom.Value.Date <= DateTime.Today)
                         && (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= DateTime.Today))
                .OrderBy(x => x.JobRoleTitle)
                .Select(x => new JobRoleViewModel
                {
                    Id = x.Id,
                    QPCode = x.QPCode,
                    JobRoleTitle = x.JobRoleTitle,
                    Description = x.Description,
                    SubSectorId = x.SubSectorId,
                    QPVersion = x.QPVersion,
                    NSQFLevel = x.NSQFLevel,
                    EducationQualification = x.EducationQualification,
                    QPHours = x.QPHours,
                    ValidFrom = x.ValidFrom,
                    ValidTo = x.ValidTo
                })
                .ToListAsync();

            return View(jobRoles);
        }
        private async Task LoadSchemes(TrainingCenterViewModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.Scheme = await _context.Scheme
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SchemeName)
                .Select(x => new SelectListItem
                {
                    Value = x.SchemeId.ToString(),
                    Text = x.SchemeCode
                })
                .ToListAsync();
        }
        [HttpGet]
        public async Task<IActionResult> Create(int jobRoleId,int? schemeId, int? projectId)
        {
            var jobRole = await _context.JobRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == jobRoleId &&
                    x.Status == "Approved" &&
                    x.IsActive &&
                    (!x.ValidFrom.HasValue ||
                     x.ValidFrom.Value.Date <= DateTime.Today) &&
                    (!x.ValidTo.HasValue ||
                     x.ValidTo.Value.Date >= DateTime.Today));

            if (jobRole == null)
            {
                TempData["Error"] = "Selected job role is unavailable.";
                return RedirectToAction(nameof(SelectJobRole));
            }

            var model = new TrainingCenterViewModel
            {
                JobRoleId = jobRole.Id,
                JobRoleTitle = jobRole.JobRoleTitle,
                SchemeId = schemeId,
                ProjectId = projectId
            };

            await LoadJobRoles(model);
            await LoadSchemes(model);

            return View(model);
        }

        private async Task LoadJobRoles(TrainingCenterViewModel model)
        {
            model.JobRoles = await _context.JobRoles
                .AsNoTracking()
                .Where(x => x.Status == "Approved" && x.IsActive)
                .OrderBy(x => x.JobRoleTitle)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.JobRoleTitle
                })
                .ToListAsync();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrainingCenterViewModel model)
        {
            var uploadedFiles = new List<string>();

            try
            {
                // 1. Validate Job Role
                var jobRole = await _context.JobRoles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.JobRoleId &&
                        x.Status == "Approved" &&
                        x.IsActive &&
                        (!x.ValidFrom.HasValue ||
                         x.ValidFrom.Value.Date <= DateTime.Today) &&
                        (!x.ValidTo.HasValue ||
                         x.ValidTo.Value.Date >= DateTime.Today));

                if (jobRole == null)
                {
                    ModelState.AddModelError(
                        nameof(model.JobRoleId),
                        "Please select a valid approved job role.");
                }

                model.JobRoleTitle = jobRole?.JobRoleTitle;

                // 2. Validate uploaded image files
                var imageFiles = new Dictionary<string, IFormFile?>
                {
                    [nameof(model.ApproachRoadPhoto)] = model.ApproachRoadPhoto,
                    [nameof(model.OutsideClassroomPhoto)] = model.OutsideClassroomPhoto,
                    [nameof(model.InsideClassroomPhoto)] = model.InsideClassroomPhoto,
                    [nameof(model.LabPhoto)] = model.LabPhoto,
                    [nameof(model.CCTVPhoto)] = model.CCTVPhoto
                };

                var allowedImageExtensions = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

                foreach (var item in imageFiles)
                {
                    var file = item.Value;

                    if (file == null || file.Length == 0)
                        continue;

                    var extension = Path.GetExtension(file.FileName);

                    if (!allowedImageExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            item.Key,
                            "Only JPG, JPEG, PNG and WEBP images are allowed.");
                    }

                    if (file.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(
                            item.Key,
                            "Image size cannot exceed 5 MB.");
                    }
                }

                // Company Seal upload validation
                if (model.CompanySeal != null &&
                    model.CompanySeal.Length > 0)
                {
                    var extension = Path.GetExtension(
                        model.CompanySeal.FileName);

                    var allowedSealExtensions = new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".webp", ".pdf"
            };

                    if (!allowedSealExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            nameof(model.CompanySeal),
                            "Company Seal must be an image or PDF.");
                    }

                    if (model.CompanySeal.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(
                            nameof(model.CompanySeal),
                            "Company Seal cannot exceed 5 MB.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    await LoadJobRoles(model);
                    return View(model);
                }

                // 3. Upload files
                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "Uploads",
                    "TrainingCenters");

                Directory.CreateDirectory(uploadFolder);

                async Task<string?> SaveFileAsync(IFormFile? file)
                {
                    if (file == null || file.Length == 0)
                        return null;

                    var extension = Path.GetExtension(file.FileName)
                        .ToLowerInvariant();

                    var fileName = $"{Guid.NewGuid():N}{extension}";
                    var physicalPath = Path.Combine(uploadFolder, fileName);

                    await using (var stream = new FileStream(
                        physicalPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
                    {
                        await file.CopyToAsync(stream);
                    }

                    uploadedFiles.Add(physicalPath);

                    return "/Uploads/TrainingCenters/" + fileName;
                }

                var approachRoadPath =
                    await SaveFileAsync(model.ApproachRoadPhoto);

                var outsideClassroomPath =
                    await SaveFileAsync(model.OutsideClassroomPhoto);

                var insideClassroomPath =
                    await SaveFileAsync(model.InsideClassroomPhoto);

                var labPhotoPath =
                    await SaveFileAsync(model.LabPhoto);

                var cctvPhotoPath =
                    await SaveFileAsync(model.CCTVPhoto);

                var companySealPath =
                    await SaveFileAsync(model.CompanySeal);

                // 4. Create database entity
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Challenge();

                // Find employee linked to the logged-in Identity user
                var TrainingPartner = await _context.TPRegistrations
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

                if (TrainingPartner == null)
                {
                    TempData["Error"] =
                        "Employee record not found for the logged-in user.";

                    return RedirectToAction(nameof(CenterList));
                }
                var center = new TSSC.Unified.Models.TrainingCenter
                {
                    SchemeId = model.SchemeId,
                    ProjectId = model.ProjectId,
                    JobRoleId = model.JobRoleId,

                    TrainingCenterName = model.TrainingCenterName,
                    TrainingCenterCode = model.TrainingCenterCode,
                    TrainingCenterSPOCName = model.TrainingCenterSPOCName,
                    TrainingCenterSPOCEmail = model.TrainingCenterSPOCEmail,
                    SPOCContactNumber = model.SPOCContactNumber,
                    TrainingCenterPhoneNumber = model.TrainingCenterPhoneNumber,

                    CompleteAddress = model.CompleteAddress,
                    CenterFloor = model.CenterFloor,
                    Landmark = model.Landmark,
                    CenterOwnershipType = model.CenterOwnershipType,
                    FranchisePartnerName = model.FranchisePartnerName,
                    CenterType = model.CenterType,

                    SelfFunded = model.SelfFunded,
                    NSDC = model.NSDC,
                    STAR = model.STAR,
                    MoRD = model.MoRD,
                    StateGovernmentSchemes = model.StateGovernmentSchemes,
                    OtherCentralGovernmentSchemes =
                        model.OtherCentralGovernmentSchemes,

                    UsableAreaSqFt = model.UsableAreaSqFt,
                    NumberOfClassrooms = model.NumberOfClassrooms,
                    NumberOfFloors = model.NumberOfFloors,
                    NumberOfLabs = model.NumberOfLabs,
                    CenterDescription = model.CenterDescription,
                    EquipmentDetails = model.EquipmentDetails,

                    // File path columns in the entity
                    ApproachRoadPhotoPath = approachRoadPath,
                    OutsideClassroomPhotoPath = outsideClassroomPath,
                    InsideClassroomPhotoPath = insideClassroomPath,
                    LabPhotoPath = labPhotoPath,
                    CCTVPhotoPath = cctvPhotoPath,
                    CompanySealPath = companySealPath,

                    OrganizationName = model.OrganizationName,
                    SigningAuthorityName = model.SigningAuthorityName,
                    MobileNumber = model.MobileNumber,
                    PANNumber = model.PANNumber,
                    EmailId = model.EmailId,
                    CreatedBy = TrainingPartner.Email,
                    Status = "Pending"

                };

                // 5. Save record
                _context.TrainingCenter.Add(center);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Training Center submitted successfully for approval.";

                return RedirectToAction(nameof(SelectJobRole));
            }
            catch (Exception ex)
            {
                foreach (var filePath in uploadedFiles)
                {
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                    }
                }
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save Training Center. Please check the details and try again.");

                await LoadJobRoles(model);
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> CenterList()
        {
            var centers = await (
                from center in _context.TrainingCenter.AsNoTracking()

                join jobRole in _context.JobRoles.AsNoTracking()
                    on center.JobRoleId equals jobRole.Id

                join schemeData in _context.Scheme.AsNoTracking()
                    on center.SchemeId equals (int?)schemeData.SchemeId
                    into schemeGroup

                from scheme in schemeGroup.DefaultIfEmpty()

                orderby center.CreatedDate descending

                select new TrainingCenterViewModel
                {
                    Id = center.Id,
                    TrainingCenterName = center.TrainingCenterName,

                    JobRoleId = center.JobRoleId,
                    JobRoleTitle = jobRole.JobRoleTitle,
                    QPHours = jobRole.QPHours,

                    SchemeId = center.SchemeId,
                    SchemeName = scheme != null
                        ? scheme.SchemeCode
                        : "-",

                    Status = center.Status,
                    PaymentStatus = center.PaymentStatus,
                    InspectionDate = center.InspectionDate
                }
            ).ToListAsync();

            return View(centers);
        }
        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var center = await _context.TrainingCenter
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
                return NotFound();

            var model = new TrainingCenterViewModel
            {
                Id = center.Id,
                TrainingCenterName = center.TrainingCenterName,
                PaymentStatus = center.PaymentStatus
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(int id,TrainingCenterViewModel model)
        {
            var center = await _context.TrainingCenter
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
                return NotFound();

            if (string.Equals(center.PaymentStatus, "Success",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Payment is already recorded.";
                return RedirectToAction(nameof(CenterList));
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            // Find employee linked to the logged-in Identity user
            var TrainingPartner = await _context.TPRegistrations
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id);

            if (TrainingPartner == null)
            {
                TempData["Error"] =
                    "Employee record not found for the logged-in user.";

                return RedirectToAction(nameof(CenterList));
            }

            // Save payment details
            center.PaymentBy = TrainingPartner.Id.ToString();
            center.PaymentDate = DateTime.Now;
            center.PaymentStatus = "Success";
            center.FinanceStatus = "Pending";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment details saved successfully.";

            return RedirectToAction(nameof(CenterList));
        }
        //[HttpGet]
        //public async Task<IActionResult> PaymentApproval(string tab = "Pending")
        //{
        //    var centers = await _context.TrainingCenter
        //        .AsNoTracking()
        //        .Where(x =>
        //            x.PaymentStatus == "Success" &&
        //            x.FinanceApprovedBy == null &&
        //            x.FinanceStatus == "Pending")
        //        .OrderByDescending(x => x.PaymentDate)
        //        .Select(x => new TrainingCenterViewModel
        //        {
        //            Id = x.Id,
        //            TrainingCenterName = x.TrainingCenterName,
        //            PaymentStatus = x.PaymentStatus,
        //            PaymentDate = x.PaymentDate,
        //            PaymentBy = x.PaymentBy,
        //            FinanceRemark = x.FinanceRemark,
        //            FinanceApprovedBy = x.FinanceApprovedBy,
        //            FinanceApprovedDate = x.FinanceApprovedDate,
        //            FinanceStatus = x.FinanceStatus,
        //        })
        //        .ToListAsync();

        //    ViewBag.Tab = tab;

        //    return View(centers);
        //}
    }
}
