using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
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
        // =====================================================
        // for TOT?TOA Employee
        // =====================================================
        public async Task<IActionResult> RegistrationList(string? status)
        {
            var query = _context.TrainerRegistration
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            var registrations = await query
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            ViewBag.Status = status;

            return View(registrations);
        }
        public async Task<IActionResult> Details(int id)
        {
            var registration = await _context.TrainerRegistration
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null)
            {
                return NotFound("Trainer registration not found.");
            }

            ViewBag.Qualifications = await _context.TrainerRegistrationQualification
                .AsNoTracking()
                .Where(x => x.TrainerRegistrationId == id)
                .ToListAsync();

            ViewBag.Experiences = await _context.TrainerRegistrationExperience
                .AsNoTracking()
                .Where(x => x.TrainerRegistrationId == id)
                .ToListAsync();

            ViewBag.Documents = await _context.TrainerRegistrationDocument
                .AsNoTracking()
                .Where(x => x.TrainerRegistrationId == id)
                .ToListAsync();

            return View(registration);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRegistration(int id, string remark)
        {
            var registration = await _context.TrainerRegistration
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null)
            {
                return NotFound("Trainer registration not found.");
            }
            //registration.Remarks = remark.Trim();
            registration.Status = "Approved";
            registration.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Trainer registration approved successfully.";

            return RedirectToAction(nameof(RegistrationList));
        }


        // ============================================
        // REJECT TRAINER REGISTRATION
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRegistration(int id, string remark)
        {
            var registration = await _context.TrainerRegistration
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null)
            {
                return NotFound("Trainer registration not found.");
            }
            //registration.Remarks = remark.Trim();
            registration.Status = "Rejected";
            registration.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Trainer registration rejected successfully.";

            return RedirectToAction(nameof(RegistrationList));
        }
        [HttpGet]
        public async Task<IActionResult> GetTrainersByJobRole(int jobRoleId)
        {
            var trainers = await _context.TrainerRegistration
                .AsNoTracking()
                .Where(x =>
                    x.JobRoleId == jobRoleId &&
                    x.Status == "Approved" &&
                    !_context.BatchMasterTrainers
                        .Any(bt => bt.TrainerRegistrationId == x.Id)
                )
                .OrderBy(x => x.CandidateName)
                .Select(x => new
                {
                    id = x.Id,
                    registrationNo = x.RegistrationNo,
                    trainerName = x.CandidateName,
                    email = x.Email,
                    mobile = x.Mobile
                })
                .ToListAsync();

            return Json(trainers);
        }
        [HttpGet]
        public async Task<IActionResult> CreateBatch()
        {
            var model = new BatchCreateVM();

            // Job Role Dropdown
            model.JobRoleList = await _context.JobRoles
                .AsNoTracking()
                .OrderBy(x => x.JobRoleTitle)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.JobRoleTitle
                })
                .ToListAsync();

            // Training Partner Dropdown
            model.TPList = await _context.TPRegistrations
         .AsNoTracking()
         .OrderBy(x => x.OrganizationName)
         .Select(x => new SelectListItem
         {
             Value = x.Id.ToString(),
             Text = x.OrganizationName
         })
         .ToListAsync();
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch(BatchCreateVM model)
        {
            // Reload dropdowns if validation fails
            async Task LoadDropdowns()
            {
                model.JobRoleList = await _context.JobRoles
                    .AsNoTracking()
                    .OrderBy(x => x.JobRoleTitle)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.JobRoleTitle
                    })
                    .ToListAsync();

                // TPList = ...
                model.TPList = await _context.TPRegistrations
           .AsNoTracking()
           .OrderBy(x => x.OrganizationName)
           .Select(x => new SelectListItem
           {
               Value = x.Id.ToString(),
               Text = x.OrganizationName
           })
           .ToListAsync();
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }

            if (model.SelectedTrainerIds == null ||
                !model.SelectedTrainerIds.Any())
            {
                ModelState.AddModelError(
                    "SelectedTrainerIds",
                    "Please select at least one trainer."
                );

                await LoadDropdowns();
                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate Job Role
                var jobRoleExists = await _context.JobRoles
                    .AnyAsync(x => x.Id == model.JobRoleId);

                if (!jobRoleExists)
                {
                    ModelState.AddModelError(
                        "JobRoleId",
                        "Selected Job Role is invalid."
                    );

                    await transaction.RollbackAsync();
                    await LoadDropdowns();
                    return View(model);
                }

                // Create Batch
                var batch = new BatchMaster
                {
                    
                    BatchName = model.BatchName.Trim(),

                    JobRoleId = model.JobRoleId!.Value,

                    TPId = model.TPId,

                    StartDate = model.StartDate,

                    EndDate = model.EndDate,
                    BatchMasterTrainer = model.BatchMasterTrainer,
                    Status = "Active",

                    CreatedDate = DateTime.Now,

                    UpdatedDate = null,

                    CreatedBy = _userManager.GetUserId(User),
                    UpdatedBy = null
                };

                _context.BatchMaster.Add(batch);

                await _context.SaveChangesAsync();

                // Save selected trainers
                foreach (var trainerId in model.SelectedTrainerIds.Distinct())
                {
                    var trainerExists = await _context.TrainerRegistration
                        .AnyAsync(x =>
                            x.Id == trainerId &&
                            x.JobRoleId == model.JobRoleId &&
                            x.Status == "Approved");

                    if (!trainerExists)
                    {
                        continue;
                    }

                    var batchTrainer = new BatchMasterTrainer
                    {
                        BatchId = batch.Id,
                        TrainerRegistrationId = trainerId,
                        CreatedDate = DateTime.Now
                    };

                    _context.BatchMasterTrainers.Add(batchTrainer);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["msg"] =
                    $"Batch '{batch.BatchName}' created successfully.";

                return RedirectToAction(
                    "BatchList",
                    "Trainer",
                    new
                    {
                        area = "HRMS"
                    });
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Ignore rollback exception
                }

                _logger.LogError(
                    ex,
                    "Error while creating batch."
                );

                ModelState.AddModelError(
                    "",
                    "Unable to create batch. Please try again."
                );

                await LoadDropdowns();
                return View(model);
            }
        }
        // ============================================
        // BATCH LIST
        // ============================================

        public async Task<IActionResult> BatchList()
        {
            var batches = await _context.BatchMaster
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(batches);
        }


        // ============================================
        // GET TRAINERS OF A BATCH
        // ============================================

        [HttpGet]
        public async Task<IActionResult> GetBatchTrainers(int batchId)
        {
            var trainers = await _context.BatchMasterTrainers
                .AsNoTracking()
                .Where(x => x.BatchId == batchId)
                .OrderBy(x => x.TrainerRegistration.CandidateName)
                .Select(x => new
                {
                    id = x.TrainerRegistrationId,

                    registrationNo = x.TrainerRegistration.RegistrationNo,

                    trainerName = x.TrainerRegistration.CandidateName,

                    email = x.TrainerRegistration.Email,

                    mobile = x.TrainerRegistration.Mobile,

                    // Latest screening
                    screening = _context.ScreeningSchedule
                        .Where(s =>
                            s.BatchId == x.BatchId &&
                            s.TrainerRegistrationId == x.TrainerRegistrationId)
                        .OrderByDescending(s => s.Id)
                        .Select(s => new
                        {
                            id = s.Id,
                            status = s.Status,
                            screeningDate = s.ScreeningDate,
                            startTime = s.StartTime,
                            endTime = s.EndTime,
                            remarks = s.Remarks,
                            emailSent = s.EmailSent
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var now = DateTime.Now;

            var result = trainers.Select(x =>
            {
                bool canUpdateResult = false;

                if (x.screening != null &&
                    x.screening.status == "Scheduled" &&
                    x.screening.endTime.HasValue)
                {
                    var screeningEndDateTime =
                        x.screening.screeningDate.Date
                        .Add(x.screening.endTime.Value);

                    canUpdateResult = now >= screeningEndDateTime;
                }

                return new
                {
                    x.id,
                    x.registrationNo,
                    x.trainerName,
                    x.email,
                    x.mobile,

                    screeningId = x.screening?.id,
                    screeningStatus = x.screening?.status,

                    screeningDate = x.screening?.screeningDate,

                    startTime = x.screening?.startTime,
                    endTime = x.screening?.endTime,

                    remarks = x.screening?.remarks,

                    emailSent = x.screening?.emailSent ?? false,

                    canUpdateResult = canUpdateResult
                };
            }).ToList();

            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> CreateScreening( int batchId, int trainerId)
        {
            var trainerMapping = await _context.BatchMasterTrainers
                .AsNoTracking()
                .Include(x => x.Batch)
                .Include(x => x.TrainerRegistration)
                .FirstOrDefaultAsync(x =>
                    x.BatchId == batchId &&
                    x.TrainerRegistrationId == trainerId);

            if (trainerMapping == null)
                return NotFound("Trainer is not assigned to this batch.");

            var model = new ScreeningScheduleVM
            {
                BatchId = batchId,
                TrainerRegistrationId = trainerId
            };

            model.BatchList = new List<SelectListItem>
    {
        new SelectListItem
        {
            Value = batchId.ToString(),
            Text = trainerMapping.Batch?.BatchName ?? "",
            Selected = true
        }
    };

            model.TrainerList = new List<SelectListItem>
    {
        new SelectListItem
        {
            Value = trainerId.ToString(),
            Text = trainerMapping.TrainerRegistration?.CandidateName
                   + " (" +
                   trainerMapping.TrainerRegistration?.RegistrationNo +
                   ")",
            Selected = true
        }
    };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> GetBatchTrainersForScreening(int batchId)
        {
            var trainers = await _context.BatchMasterTrainers
                .AsNoTracking()
                .Where(x => x.BatchId == batchId)
                .OrderBy(x => x.TrainerRegistration.CandidateName)
                .Select(x => new
                {
                    id = x.TrainerRegistrationId,
                    registrationNo = x.TrainerRegistration.RegistrationNo,
                    trainerName = x.TrainerRegistration.CandidateName,
                    email = x.TrainerRegistration.Email,
                    mobile = x.TrainerRegistration.Mobile
                })
                .ToListAsync();

            return Json(trainers);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateScreening(ScreeningScheduleVM model)
        {
            // =====================================================
            // LOAD DROPDOWNS
            // =====================================================
            async Task LoadDropdowns()
            {
                model.BatchList = await _context.BatchMaster
                    .AsNoTracking()
                    .Where(x => x.Status == "Active")
                    .OrderByDescending(x => x.Id)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.BatchName
                    })
                    .ToListAsync();

                model.TrainerList = new List<SelectListItem>();

                if (model.BatchId.HasValue)
                {
                    model.TrainerList = await _context.BatchMasterTrainers
                        .AsNoTracking()
                        .Include(x => x.TrainerRegistration)
                        .Where(x => x.BatchId == model.BatchId.Value)
                        .OrderBy(x => x.TrainerRegistration.CandidateName)
                        .Select(x => new SelectListItem
                        {
                            Value = x.TrainerRegistrationId.ToString(),
                            Text = x.TrainerRegistration.CandidateName
                                   + " (" +
                                   x.TrainerRegistration.RegistrationNo +
                                   ")"
                        })
                        .ToListAsync();
                }
            }


            // =====================================================
            // 1. MODEL VALIDATION
            // =====================================================
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }


            // =====================================================
            // 2. CHECK BATCH + TRAINER MAPPING
            // =====================================================
            var batchTrainer = await _context.BatchMasterTrainers
                .AsNoTracking()
                .Include(x => x.Batch)
                .Include(x => x.TrainerRegistration)
                .FirstOrDefaultAsync(x =>
                    x.BatchId == model.BatchId!.Value &&
                    x.TrainerRegistrationId == model.TrainerRegistrationId!.Value
                );

            if (batchTrainer == null)
            {
                ModelState.AddModelError(
                    "TrainerRegistrationId",
                    "Selected trainer is not assigned to the selected batch."
                );

                await LoadDropdowns();
                return View(model);
            }


            // =====================================================
            // 3. GET TRAINER + BATCH
            // =====================================================
            var trainer = batchTrainer.TrainerRegistration;
            var batch = batchTrainer.Batch;


            // =====================================================
            // 4. DATE / TIME VALIDATION
            // =====================================================
            if (model.EndTime.HasValue &&
                model.EndTime.Value <= model.StartTime!.Value)
            {
                ModelState.AddModelError(
                    "EndTime",
                    "End Time must be greater than Start Time."
                );

                await LoadDropdowns();
                return View(model);
            }


            // =====================================================
            // 5. ONLINE VALIDATION
            // =====================================================
            if (model.ScreeningMode == "Online" &&
                string.IsNullOrWhiteSpace(model.MeetingLink))
            {
                ModelState.AddModelError(
                    "MeetingLink",
                    "Meeting Link is required for online screening."
                );

                await LoadDropdowns();
                return View(model);
            }


            // =====================================================
            // 6. OFFLINE VALIDATION
            // =====================================================
            if (model.ScreeningMode == "Offline" &&
                string.IsNullOrWhiteSpace(model.Location))
            {
                ModelState.AddModelError(
                    "Location",
                    "Location is required for offline screening."
                );

                await LoadDropdowns();
                return View(model);
            }


            // =====================================================
            // 7. DUPLICATE SCREENING CHECK
            // =====================================================
            var alreadyScheduled = await _context.ScreeningSchedule
                .AnyAsync(x =>
                    x.BatchId == model.BatchId.Value &&
                    x.TrainerRegistrationId == model.TrainerRegistrationId.Value &&
                    x.ScreeningDate == model.ScreeningDate.Value &&
                    x.StartTime == model.StartTime.Value &&
                    x.Status != "Cancelled"
                );

            if (alreadyScheduled)
            {
                TempData["error"] =
                    "Screening is already scheduled for this trainer on the selected date and time.";

                await LoadDropdowns();
                return View(model);
            }


            // =====================================================
            // 8. CREATE SCREENING
            // =====================================================
            var screening = new ScreeningSchedule
            {
                BatchId = model.BatchId.Value,

                TrainerRegistrationId =
                    model.TrainerRegistrationId.Value,

                ScreeningDate =
                    model.ScreeningDate.Value,

                StartTime =
                    model.StartTime.Value,

                EndTime =
                    model.EndTime,

                ScreeningMode =
                    model.ScreeningMode?.Trim(),

                Location =
                    model.ScreeningMode == "Offline"
                        ? model.Location?.Trim()
                        : null,

                MeetingLink =
                    model.ScreeningMode == "Online"
                        ? model.MeetingLink?.Trim()
                        : null,

                Status = "Scheduled",

                Remarks =
                    string.IsNullOrWhiteSpace(model.Remarks)
                        ? null
                        : model.Remarks.Trim(),

                CreatedDate = DateTime.Now
            };

            _context.ScreeningSchedule.Add(screening);

            await _context.SaveChangesAsync();


            // =====================================================
            // 9. SEND EMAIL TO TRAINER
            // =====================================================
            if (trainer != null &&
                !string.IsNullOrWhiteSpace(trainer.Email))
            {
                try
                {
                    string templatePath = Path.Combine(
                        _environment.WebRootPath,
                        "EmailTemplates",
                        "TrainerScreeningSchedule.html"
                    );

                    string baseUrl =
                        $"{Request.Scheme}://{Request.Host}";


                    // ---------------------------------------------
                    // EMAIL REPLACEMENTS
                    // ---------------------------------------------
                    var replacements =
                        new Dictionary<string, string>
                        {
                    {
                        "BaseUrl",
                        baseUrl
                    },

                    {
                        "TrainerName",
                        trainer.CandidateName ?? ""
                    },

                    {
                        "RegistrationNo",
                        trainer.RegistrationNo ?? ""
                    },

                    {
                        "BatchName",
                        batch?.BatchName ?? ""
                    },

                    {
                        "ScreeningDate",
                        model.ScreeningDate.Value
                            .ToString("dd-MM-yyyy")
                    },

                    {
                        "StartTime",
                        model.StartTime.Value
                            .ToString(@"hh\:mm")
                    },

                    {
                        "EndTime",
                        model.EndTime.HasValue
                            ? model.EndTime.Value
                                .ToString(@"hh\:mm")
                            : "N/A"
                    },

                    {
                        "ScreeningMode",
                        model.ScreeningMode ?? ""
                    },

                    {
                        "Location",
                        model.ScreeningMode == "Offline"
                            ? model.Location ?? ""
                            : ""
                    },

                    {
                        "MeetingLink",
                        model.ScreeningMode == "Online"
                            ? model.MeetingLink ?? ""
                            : ""
                    },

                    {
                        "Remarks",
                        string.IsNullOrWhiteSpace(model.Remarks)
                            ? "N/A"
                            : model.Remarks.Trim()
                    }
                        };


                    // ---------------------------------------------
                    // SEND EMAIL
                    // ---------------------------------------------
                    string result =
                        await _emailService.SendEmailAsync(
                            trainer.Email,
                            "Trainer Screening Scheduled - TSSC",
                            templatePath,
                            replacements
                        );


                    if (result == "True")
                    {
                        screening.EmailSent = true;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation(
                            "Screening email sent successfully to {Email}",
                            trainer.Email
                        );
                    }
                    else
                    {
                        _logger.LogError(
                            "Screening email failed for {Email}. Error: {Error}",
                            trainer.Email,
                            result
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Screening is already saved.
                    // Do NOT rollback screening because of email failure.

                    _logger.LogError(
                        ex,
                        "Unable to send screening schedule email to {Email}",
                        trainer.Email
                    );
                }
            }


            // =====================================================
            // 10. SUCCESS
            // =====================================================
            TempData["msg"] =
                "Screening scheduled successfully.";

            return RedirectToAction(nameof(BatchList));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateScreeningResult(
     int id,
     string status,
     string remarks)
        {
            var screening = await _context.ScreeningSchedule
                .Include(x => x.TrainerRegistration)
                .Include(x => x.Batch)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (screening == null)
            {
                TempData["error"] = "Screening record not found.";
                return RedirectToAction(nameof(BatchList));
            }

            if (!string.Equals(
                    screening.Status,
                    "Scheduled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Screening result has already been updated.";
                return RedirectToAction(nameof(BatchList));
            }

            var validStatuses = new[]
            {
        "Success",
        "Rejected",
        "On Hold"
    };

            if (!validStatuses.Contains(status))
            {
                TempData["error"] = "Please select a valid screening result.";
                return RedirectToAction(nameof(BatchList));
            }

            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["error"] = "Screening remarks are required.";
                return RedirectToAction(nameof(BatchList));
            }

            if (!screening.EndTime.HasValue)
            {
                TempData["error"] = "Screening end time is not available.";
                return RedirectToAction(nameof(BatchList));
            }

            var screeningEndDateTime =
                screening.ScreeningDate.Date.Add(screening.EndTime.Value);

            if (DateTime.Now < screeningEndDateTime)
            {
                TempData["error"] =
                    "Screening result can be updated only after the screening end time.";

                return RedirectToAction(nameof(BatchList));
            }

            // Screening update
            screening.Status = status.Trim();
            screening.Remarks = remarks.Trim();
            screening.UpdatedDate = DateTime.Now;

            // Trainer update
            if (screening.TrainerRegistration != null)
            {
                switch (status.Trim())
                {
                    case "Success":
                        screening.TrainerRegistration.Status = "Approved";
                        screening.TrainerRegistration.IsActive = true;
                        break;

                    case "Rejected":
                        screening.TrainerRegistration.Status = "Rejected";
                        screening.TrainerRegistration.IsActive = false;
                        break;

                    case "On Hold":
                        screening.TrainerRegistration.Status = "On Hold";
                        screening.TrainerRegistration.IsActive = true;
                        break;
                }

                screening.TrainerRegistration.UpdatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["msg"] =
                $"Screening result updated successfully as {status}.";

            return RedirectToAction(nameof(BatchList));
        }
        [HttpGet]
        public async Task<IActionResult> PaymentApproval(string tab = "Pending")
        {
            var query = _context.TrainerRegistration
                .AsNoTracking()
                .Where(x =>
                    x.PaymentStatus == "Success" &&
                    x.PaymentVerified);

            if (string.Equals(tab, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.FinanceApproved);
            }
            else
            {
                query = query.Where(x => !x.FinanceApproved);
                tab = "Pending";
            }

            var trainers = await query
                .OrderByDescending(x => x.PaymentDate)
                .Select(x => new TrainerRegistrationViewModel
                {
                    Id = x.Id,
                    CandidateName = x.CandidateName,
                    JobRoleId = x.JobRoleId,

                    JobRoleName = _context.JobRoles
                        .Where(j => j.Id == x.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault(),

                    Email = x.Email ?? string.Empty,

                    PaymentStatus = x.PaymentStatus,
                    PaymentDate = x.PaymentDate,
                    PaymentVerified = x.PaymentVerified,
                    FinanceApproved = x.FinanceApproved,
                    FinanceApprovedDate = x.FinanceApprovedDate
                })
                .ToListAsync();

            ViewBag.ActiveTab = tab;

            return View(trainers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePayment(int id)
        {
            var trainer = await _context.TrainerRegistration
                .FirstOrDefaultAsync(x => x.Id == id);

            if (trainer == null)
            {
                TempData["error"] = "Trainer not found.";

                return RedirectToAction(
                    nameof(PaymentApproval),
                    new { tab = "Pending" });
            }

            if (!trainer.PaymentVerified ||
                !string.Equals(
                    trainer.PaymentStatus,
                    "Success",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] =
                    "Payment must be verified before Finance approval.";

                return RedirectToAction(
                    nameof(PaymentApproval),
                    new { tab = "Pending" });
            }

            trainer.FinanceApproved = true;
            trainer.FinanceApprovedDate = DateTime.Now;
            trainer.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Payment approved by Finance successfully.";

            return RedirectToAction(
                nameof(PaymentApproval),
                new { tab = "Approved" });
        }
        [HttpGet]
        public async Task<IActionResult> AssignAgencyToBatch()
        {
            var batches = await _context.BatchMaster
                .AsNoTracking()
                .Include(x => x.JobRole)
                .Include(x => x.AssessmentAgency)
                .Where(x =>
                    _context.BatchMasterTrainers.Any(bt =>
                        bt.BatchId == x.Id &&
                        bt.TrainerRegistration != null &&
                        bt.TrainerRegistration.FinanceApproved &&
                        bt.TrainerRegistration.PaymentVerified &&
                        bt.TrainerRegistration.IsActive
                    ))
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var agencies = await _context.AssessmentAgency
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.AgencyName)
                .ToListAsync();

            ViewBag.Agencies = agencies;

            return View(batches);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAgencyToBatch(int batchId,int assessmentAgencyId)
        {
            var batch = await _context.BatchMaster
                .FirstOrDefaultAsync(x => x.Id == batchId);

            if (batch == null)
            {
                TempData["error"] = "Batch not found.";
                return RedirectToAction(nameof(AssignAgencyToBatch), new { batchId });
            }

            var agency = await _context.AssessmentAgency
                .FirstOrDefaultAsync(x =>
                    x.Id == assessmentAgencyId &&
                    x.IsActive);

            if (agency == null)
            {
                TempData["error"] = "Please select a valid active agency.";

                return RedirectToAction(
                    nameof(AssignAgencyToBatch),
                    new { batchId });
            }

            // Existing batch row update
            batch.AssessmentAgencyId = agency.Id;
            batch.AssignedBy = _userManager.GetUserId(User);
            batch.AssignedDate = DateTime.Now;

            // Send request to Vertical Head
            batch.AgencyApprovalStatus = "Pending";

            batch.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Assessment Agency request sent to Vertical Head for approval.";

            return RedirectToAction(nameof(AssignAgencyToBatch), new { batchId });
        }

        [HttpGet]
        public async Task<IActionResult> AgencyApprovalByManager()
        {
            var batches = await _context.BatchMaster
                .AsNoTracking()
                .Include(x => x.JobRole)
                .Include(x => x.AssessmentAgency)
                .Where(x =>
                    x.AssessmentAgencyId != null &&
                    x.AgencyApprovalStatus == "Pending")
                .OrderByDescending(x => x.AssignedDate)
                .ToListAsync();

            return View(batches);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgencyApprovalByManager(int id)
        {
            var batch = await _context.BatchMaster
                .FirstOrDefaultAsync(x => x.Id == id);

            if (batch == null)
            {
                TempData["error"] = "Batch not found.";
                return RedirectToAction(nameof(AgencyApprovalByManager));
            }

            if (batch.AssessmentAgencyId == null)
            {
                TempData["error"] = "Assessment Agency is not assigned.";
                return RedirectToAction(nameof(AgencyApprovalByManager));
            }

            if (!string.Equals(
                    batch.AgencyApprovalStatus,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This agency request has already been processed.";
                return RedirectToAction(nameof(AgencyApprovalByManager));
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                TempData["error"] = "Logged-in user not found.";
                return RedirectToAction(nameof(AgencyApprovalByManager));
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                TempData["error"] = "Employee record not found.";
                return RedirectToAction(nameof(AgencyApprovalByManager));
            }
            batch.AgencyApprovalStatus = "Approved";
            batch.VerticalHeadApprovedBy = employee.EmployeeId.ToString();
            batch.VerticalHeadApprovedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // GET TRAINER / TOA FOR THIS BATCH
            // =====================================================

            var trainers = await _context.BatchMasterTrainers
                .AsNoTracking()
                .Include(x => x.TrainerRegistration)
                .Where(x =>
                    x.BatchId == batch.Id &&
                    x.TrainerRegistration != null &&
                    x.TrainerRegistration.IsActive)
                .Select(x => x.TrainerRegistration!)
                .ToListAsync();

            // =====================================================
            // SEND EMAIL TO TOA / TRAINER
            // =====================================================

            foreach (var trainer in trainers)
            {
                if (string.IsNullOrWhiteSpace(trainer.Email))
                    continue;

                try
                {
                    string templatePath = Path.Combine(
                        _environment.WebRootPath,
                        "EmailTemplates",
                        "TrainerBatchReadyEmail.html"
                    );
                    string baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var replacements = new Dictionary<string, string>
                {
                { "BaseUrl", baseUrl },

                { "Name",trainer.CandidateName ?? string.Empty },

                { "BatchName",batch.BatchName ?? string.Empty },

                { "BatchCode",batch.BatchCode ?? string.Empty },

                { "StartDate",batch.StartDate?.ToString("dd-MMM-yyyy") ?? "N/A" },

                { "PortalUrl",$"{baseUrl}/Trainer/Home/Index" },

                { "SupportEmail", "it@tsscindia.com" }
                };

                    string result = await _emailService.SendEmailAsync(
                        trainer.Email,
                        "Training Batch Ready - TSSC",
                        templatePath,
                        replacements
                    );

                    if (result == "True")
                    {
                        batch.EmailSent = true;
                        batch.UpdatedDate = DateTime.Now;

                        await _context.SaveChangesAsync();

                        TempData["msg"] =
                            "Assessment Agency approved and email sent successfully.";
                    }
                    else
                    {
                        batch.EmailSent = false;
                        await _context.SaveChangesAsync();

                        TempData["error"] =
                            "Assessment Agency approved, but email could not be sent.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unable to send batch ready email to {Email}",
                        trainer.Email
                    );
                }
            }


            TempData["msg"] = "Assessment Agency approved successfully.";

            return RedirectToAction(nameof(ApprovedAgency));
        }
        [HttpGet]
        public async Task<IActionResult> ApprovedAgency()
        {
            var batches = await _context.BatchMaster
                .AsNoTracking()
                .Include(x => x.JobRole)
                .Include(x => x.AssessmentAgency)
                .Where(x =>
                    x.AssessmentAgencyId != null &&
                    x.AgencyApprovalStatus == "Approved")
                .OrderByDescending(x => x.VerticalHeadApprovedDate)
                .ToListAsync();

            return View(batches);
        }
    }
}
