using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Areas.HRMS.Controllers;
using QUIZAPP.Models;
using System.Collections.Generic;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class ReimbursementsController:Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        public ReimbursementsController(ILogger<EmployeeController> logger,
            AppdbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager, IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }
        private void LoadDropdowns(ReimbursementsVM model)
        {
            model.RequestTypeList = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Travel",
                    Value = "Travel"
                },

                new SelectListItem
                {
                    Text = "Medical",
                    Value = "Medical"
                },

                new SelectListItem
                {
                    Text = "Office Expense",
                    Value = "Office Expense"
                },

                new SelectListItem
                {
                    Text = "Other",
                    Value = "Other"
                }
            };


            model.ExpenseCategoryList = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Food",
                    Value = "Food"
                },

                new SelectListItem
                {
                    Text = "Hotel",
                    Value = "Hotel"
                },

                new SelectListItem
                {
                    Text = "Transport",
                    Value = "Transport"
                },

                new SelectListItem
                {
                    Text = "Fuel",
                    Value = "Fuel"
                },
                new SelectListItem
                {
                    Text = "Miscellaneous",
                    Value = "Miscellaneous"
                }
            };


            model.PaymentModeList = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Cash",
                    Value = "Cash"
                },

                new SelectListItem
                {
                    Text = "Credit Card",
                    Value = "Credit Card"
                },

                new SelectListItem
                {
                    Text = "Debit Card",
                    Value = "Debit Card"
                },

                new SelectListItem
                {
                    Text = "UPI",
                    Value = "UPI"
                },

                new SelectListItem
                {
                    Text = "Bank Transfer",
                    Value = "Bank Transfer"
                }
            };

            model.ApproverList = _context.Employee
     .Where(x => x.IsActive)
     .Select(x => new SelectListItem
     {
         Text = x.FirstName + " " + x.LastName
                + " (" + x.EmployeeId + ")",
         Value = x.EmployeeId.ToString()
     })
     .OrderBy(x => x.Text)
     .ToList();
        }

        private async Task<string> UploadReimbursementFile(IFormFile file)
        {
            var folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "reimbursements"
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName =
                Guid.NewGuid().ToString()
                + Path.GetExtension(file.FileName);

            var filePath = Path.Combine(
                folderPath,
                fileName
            );

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/reimbursements/" + fileName;
        }

        [HttpGet]
        public async Task<IActionResult> CreateReimbursement(int? id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return NotFound("Employee record not found.");
            }

            ReimbursementsVM model;

            // =========================
            // EDIT
            // =========================
            if (id.HasValue)
            {
                var reimbursement = await _context.Reimbursements
                    .FirstOrDefaultAsync(x =>
                        x.ReimbursementId == id.Value &&
                        x.EmployeeId == employee.EmployeeId &&
                        x.IsActive);

                if (reimbursement == null)
                {
                    return NotFound("Reimbursement not found.");
                }

                model = new ReimbursementsVM
                {
                    ReimbursementId = reimbursement.ReimbursementId,
                    EmployeeId = reimbursement.EmployeeId,

                    RequestType = reimbursement.RequestType,
                    ExpenseCategory = reimbursement.ExpenseCategory,
                    ExpenseAmount = reimbursement.ExpenseAmount,
                    ExpenseDate = reimbursement.ExpenseDate,
                    VendorName = reimbursement.VendorName,
                    PaymentMode = reimbursement.PaymentMode,
                    Purpose = reimbursement.Purpose,

                    SupportingDocument = reimbursement.SupportingDocument,
                    InvoiceBill = reimbursement.InvoiceBill,

                    Remarks = reimbursement.Remarks,
                    Status = reimbursement.Status,

                    // Approver 1
                    Approver1Id = reimbursement.Approver1Id,
                    Approver1Status = reimbursement.Approver1Status,

                    // Approver 2
                    Approver2Id = reimbursement.Approver2Id,
                    Approver2Status = reimbursement.Approver2Status,

                    // Approver 3
                    Approver3Id = reimbursement.Approver3Id,
                    Approver3Status = reimbursement.Approver3Status,

                    // Approver 4
                    Approver4Id = reimbursement.Approver4Id,
                    Approver4Status = reimbursement.Approver4Status,

                    FinalStatus = reimbursement.FinalStatus,

                    CreatedOn = reimbursement.CreatedOn,
                    UpdatedOn = reimbursement.UpdatedOn,
                    IsActive = reimbursement.IsActive
                };

                // Approver 1 ka name
                if (model.Approver1Id.HasValue)
                {
                    var approver1 = await _context.Employee
                        .FirstOrDefaultAsync(x => x.EmployeeId == model.Approver1Id.Value);

                    if (approver1 != null)
                    {
                        model.Approver1Name =
                            approver1.FirstName + " " + approver1.LastName;
                    }
                }
            }
            else
            {
                // =========================
                // NEW REIMBURSEMENT
                // =========================

                model = new ReimbursementsVM
                {
                    EmployeeId = employee.EmployeeId,

                    ExpenseDate = DateTime.Today,

                    Status = "Pending",

                    // Creator = Approver 1
                    Approver1Id = employee.EmployeeId,
                    Approver1Name =
                        employee.FirstName + " " + employee.LastName,

                    Approver1Status = "Approved",

                    Approver2Status = "Pending",
                    Approver3Status = "Pending",
                    Approver4Status = "Pending",

                    FinalStatus = "Pending",

                    CreatedOn = DateTime.Now,

                    IsActive = true
                };
            }

            // IMPORTANT
            LoadDropdowns(model);

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReimbursement(ReimbursementsVM model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return NotFound("Employee record not found.");
            }

            // Logged-in employee
            model.EmployeeId = employee.EmployeeId;

            // Creator will always remain Approver 1
            model.Approver1Id = employee.EmployeeId;

            // Remove Approver 1 validation because it is automatic
            ModelState.Remove("Approver1Id");

            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);

                model.Approver1Name =
                    employee.FirstName + " " + employee.LastName;

                return View(model);
            }

            // =====================================================
            // EDIT
            // =====================================================

            if (model.ReimbursementId > 0)
            {
                var reimbursement = await _context.Reimbursements
                    .FirstOrDefaultAsync(x =>
                        x.ReimbursementId == model.ReimbursementId &&
                        x.EmployeeId == employee.EmployeeId &&
                        x.IsActive);

                if (reimbursement == null)
                {
                    return NotFound("Reimbursement not found.");
                }

                // ---------------------------------
                // Reimbursement Details
                // ---------------------------------

                reimbursement.RequestType = model.RequestType;
                reimbursement.ExpenseCategory = model.ExpenseCategory;
                reimbursement.ExpenseAmount = model.ExpenseAmount;
                reimbursement.ExpenseDate = model.ExpenseDate;
                reimbursement.VendorName = model.VendorName;
                reimbursement.PaymentMode = model.PaymentMode;
                reimbursement.Purpose = model.Purpose;
                reimbursement.Remarks = model.Remarks;

                // ---------------------------------
                // Approvers
                // ---------------------------------

                reimbursement.Approver1Id = employee.EmployeeId;

                reimbursement.Approver2Id = model.Approver2Id;
                reimbursement.Approver3Id = model.Approver3Id;
                reimbursement.Approver4Id = model.Approver4Id;

                // Agar approver change kiya gaya hai
                // to uska status Pending kar do

                reimbursement.Approver2Status =
                    model.Approver2Id.HasValue ? "Pending" : null;

                reimbursement.Approver3Status =
                    model.Approver3Id.HasValue ? "Pending" : null;

                reimbursement.Approver4Status =
                    model.Approver4Id.HasValue ? "Pending" : null;

                // Creator already approved
                reimbursement.Approver1Status = "Approved";

                reimbursement.UpdatedOn = DateTime.Now;

                // ---------------------------------
                // Supporting Document
                // ---------------------------------

                if (model.SupportingDocumentFile != null &&
                    model.SupportingDocumentFile.Length > 0)
                {
                    reimbursement.SupportingDocument =
                        await UploadReimbursementFile(
                            model.SupportingDocumentFile);
                }

                // ---------------------------------
                // Invoice
                // ---------------------------------

                if (model.InvoiceBillFile != null &&
                    model.InvoiceBillFile.Length > 0)
                {
                    reimbursement.InvoiceBill =
                        await UploadReimbursementFile(
                            model.InvoiceBillFile);
                }

                await _context.SaveChangesAsync();

                TempData["msg"] =
                    "Reimbursement updated successfully.";

                return RedirectToAction("MyReimbursement");
            }

            // =====================================================
            // CREATE
            // =====================================================

            var reimbursements = new Reimbursements
            {
                EmployeeId = employee.EmployeeId,

                RequestType = model.RequestType,
                ExpenseCategory = model.ExpenseCategory,
                ExpenseAmount = model.ExpenseAmount,
                ExpenseDate = model.ExpenseDate,
                VendorName = model.VendorName,
                PaymentMode = model.PaymentMode,
                Purpose = model.Purpose,
                Remarks = model.Remarks,

                // Overall
                Status = "Pending",

                // Approver 1 = Creator
                Approver1Id = employee.EmployeeId,
                Approver1Status = "Approved",

                // Approver 2
                Approver2Id = model.Approver2Id,
                Approver2Status =
                    model.Approver2Id.HasValue
                        ? "Pending"
                        : null,

                // Approver 3
                Approver3Id = model.Approver3Id,
                Approver3Status =
                    model.Approver3Id.HasValue
                        ? "Pending"
                        : null,

                // Approver 4
                Approver4Id = model.Approver4Id,
                Approver4Status =
                    model.Approver4Id.HasValue
                        ? "Pending"
                        : null,

                // Finance
                FinalStatus = "Pending",

                CreatedOn = DateTime.Now,
                IsActive = true
            };

            // Supporting Document
            if (model.SupportingDocumentFile != null &&
                model.SupportingDocumentFile.Length > 0)
            {
                reimbursements.SupportingDocument =
                    await UploadReimbursementFile(
                        model.SupportingDocumentFile);
            }

            // Invoice / Bill
            if (model.InvoiceBillFile != null &&
                model.InvoiceBillFile.Length > 0)
            {
                reimbursements.InvoiceBill =
                    await UploadReimbursementFile(
                        model.InvoiceBillFile);
            }

            _context.Reimbursements.Add(reimbursements);

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Reimbursement submitted successfully.";

            return RedirectToAction("MyReimbursement");
        }


        [HttpGet]
        public async Task<IActionResult> MyReimbursement()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return View(new List<ReimbursementsVM>());
            }

            var reimbursementList = await
                (from r in _context.Reimbursements

                     // Approver 1
                 join a1 in _context.Employee
                     on r.Approver1Id equals a1.EmployeeId
                     into approver1Group
                 from a1 in approver1Group.DefaultIfEmpty()

                     // Approver 2
                 join a2 in _context.Employee
                     on r.Approver2Id equals a2.EmployeeId
                     into approver2Group
                 from a2 in approver2Group.DefaultIfEmpty()

                     // Approver 3
                 join a3 in _context.Employee
                     on r.Approver3Id equals a3.EmployeeId
                     into approver3Group
                 from a3 in approver3Group.DefaultIfEmpty()

                     // Approver 4
                 join a4 in _context.Employee
                     on r.Approver4Id equals a4.EmployeeId
                     into approver4Group
                 from a4 in approver4Group.DefaultIfEmpty()

                 where r.EmployeeId == employee.EmployeeId
                       && r.IsActive

                 orderby r.CreatedOn descending

                 select new ReimbursementsVM
                 {
                     // =========================
                     // REIMBURSEMENT
                     // =========================

                     ReimbursementId = r.ReimbursementId,
                     EmployeeId = r.EmployeeId,

                     RequestType = r.RequestType,
                     ExpenseCategory = r.ExpenseCategory,
                     ExpenseAmount = r.ExpenseAmount,
                     ExpenseDate = r.ExpenseDate,
                     VendorName = r.VendorName,
                     PaymentMode = r.PaymentMode,
                     Purpose = r.Purpose,

                     SupportingDocument = r.SupportingDocument,
                     InvoiceBill = r.InvoiceBill,

                     Remarks = r.Remarks,
                     Status = r.Status,

                     CreatedOn = r.CreatedOn,

                     // =========================
                     // APPROVER 1
                     // =========================

                     Approver1Id = r.Approver1Id,

                     Approver1Name = a1 != null
                         ? a1.FirstName + " " + a1.LastName
                         : null,

                     Approver1Status = r.Approver1Status,
                     Approver1Remarks = r.Approver1Remarks,
                     Approver1ApprovedOn = r.Approver1ApprovedOn,

                     // =========================
                     // APPROVER 2
                     // =========================

                     Approver2Id = r.Approver2Id,

                     Approver2Name = a2 != null
                         ? a2.FirstName + " " + a2.LastName
                         : null,

                     Approver2Status = r.Approver2Status,
                     Approver2Remarks = r.Approver2Remarks,
                     Approver2ApprovedOn = r.Approver2ApprovedOn,

                     // =========================
                     // APPROVER 3
                     // =========================

                     Approver3Id = r.Approver3Id,

                     Approver3Name = a3 != null
                         ? a3.FirstName + " " + a3.LastName
                         : null,

                     Approver3Status = r.Approver3Status,
                     Approver3Remarks = r.Approver3Remarks,
                     Approver3ApprovedOn = r.Approver3ApprovedOn,

                     // =========================
                     // APPROVER 4
                     // =========================

                     Approver4Id = r.Approver4Id,

                     Approver4Name = a4 != null
                         ? a4.FirstName + " " + a4.LastName
                         : null,

                     Approver4Status = r.Approver4Status,
                     Approver4Remarks = r.Approver4Remarks,
                     Approver4ApprovedOn = r.Approver4ApprovedOn,

                     // =========================
                     // FINANCE
                     // =========================

                     FinalStatus = r.FinalStatus,
                     FinalRemarks = r.FinalRemarks,
                     FinalApprovedOn = r.FinalApprovedOn,

                     PaidOn = r.PaidOn
                 })
                .ToListAsync();

            return View(reimbursementList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReimbursement(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return NotFound("Employee record not found.");
            }

            var reimbursement = await _context.Reimbursements
                .FirstOrDefaultAsync(x =>
                    x.ReimbursementId == id &&
                    x.EmployeeId == employee.EmployeeId &&
                    x.IsActive);

            if (reimbursement == null)
            {
                return NotFound("Reimbursement not found.");
            }


            // =====================================
            // DELETE ONLY IF NO APPROVER HAS TAKEN ACTION
            // =====================================

            bool approver2Updated =
                reimbursement.Approver2Status == "Approved" ||
                reimbursement.Approver2Status == "Rejected";

            bool approver3Updated =
                reimbursement.Approver3Status == "Approved" ||
                reimbursement.Approver3Status == "Rejected";

            bool approver4Updated =
                reimbursement.Approver4Status == "Approved" ||
                reimbursement.Approver4Status == "Rejected";


            // Agar kisi bhi approver ne approve/reject kar diya
            if (approver2Updated || approver3Updated || approver4Updated)
            {
                TempData["error"] =
                    "Reimbursement cannot be deleted because the approval process has already been updated.";

                return RedirectToAction("MyReimbursement");
            }


            // =====================================
            // DELETE
            // =====================================

            reimbursement.IsActive = false;

            reimbursement.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Reimbursement deleted successfully.";

            return RedirectToAction("MyReimbursement");
        }

        [HttpGet]
        public async Task<IActionResult> Approval()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return NotFound("Employee record not found.");
            }
            // CEO Department
            var isCEO = employee.DepartmentId == 22;
            var employeeId = employee.EmployeeId;

            var reimbursementList = await _context.Reimbursements
                .Where(x =>
                    x.IsActive &&

                    (
                        // =========================
                        // APPROVER 2
                        // =========================
                        (
                            x.Approver2Id == employeeId &&
                            x.Approver1Status == "Approved" &&
                            x.Approver2Status == "Pending"
                        )

                        ||

                        // =========================
                        // APPROVER 3
                        // =========================
                        (
                            x.Approver3Id == employeeId &&
                            x.Approver1Status == "Approved" &&
                            x.Approver2Status == "Approved" &&
                            x.Approver3Status == "Pending"
                        )

                        ||

                        // =========================
                        // APPROVER 4
                        // =========================
                        (
                            x.Approver4Id == employeeId &&
                            x.Approver1Status == "Approved" &&
                            x.Approver2Status == "Approved" &&
                            x.Approver3Status == "Approved" &&
                            x.Approver4Status == "Pending"
                        )
                        ||
                       // =========================
                       // CEO APPROVER 
                       // =========================
                       (isCEO && x.Approver1Status == "Approved" &&
                       x.Approver2Status == "Approved" && 
                       x.Approver3Status == "Approved" &&
                       x.Approver4Status == "Approved" && 
                       x.FinalStatus == "Pending")
                    )
                )
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new ReimbursementsVM
                {
                    ReimbursementId = x.ReimbursementId,

                    EmployeeId = x.EmployeeId,

                    RequestType = x.RequestType,

                    ExpenseCategory = x.ExpenseCategory,

                    ExpenseAmount = x.ExpenseAmount,

                    ExpenseDate = x.ExpenseDate,

                    VendorName = x.VendorName,

                    PaymentMode = x.PaymentMode,

                    Purpose = x.Purpose,

                    SupportingDocument = x.SupportingDocument,

                    InvoiceBill = x.InvoiceBill,

                    Remarks = x.Remarks,

                    Status = x.Status,

                    // Approvers
                    Approver1Id = x.Approver1Id,
                    Approver1Status = x.Approver1Status,

                    Approver2Id = x.Approver2Id,
                    Approver2Status = x.Approver2Status,

                    Approver3Id = x.Approver3Id,
                    Approver3Status = x.Approver3Status,

                    Approver4Id = x.Approver4Id,
                    Approver4Status = x.Approver4Status,

                    
                    FinalStatus = x.FinalStatus,

                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();

            return View(reimbursementList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReject(int reimbursementId, string actionType, string? remarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return NotFound("Employee record not found.");
            }

            var reimbursement = await _context.Reimbursements
                .FirstOrDefaultAsync(x =>
                    x.ReimbursementId == reimbursementId &&
                    x.IsActive);

            if (reimbursement == null)
            {
                return NotFound("Reimbursement not found.");
            }

            var employeeId = employee.EmployeeId;

            var status = actionType == "Approve"
                ? "Approved"
                : "Rejected";

            // CEO Department
            var isCEO = employee.DepartmentId == 22;
            // =====================================
            // APPROVER 2
            // =====================================

            if (reimbursement.Approver2Id == employeeId &&
                reimbursement.Approver1Status == "Approved" &&
                reimbursement.Approver2Status == "Pending")
            {
                reimbursement.Approver2Status = status;

                reimbursement.Approver2Remarks = remarks;

                reimbursement.Approver2ApprovedOn = DateTime.Now;
            }


            // =====================================
            // APPROVER 3
            // =====================================

            else if (reimbursement.Approver3Id == employeeId &&
                     reimbursement.Approver1Status == "Approved" &&
                     reimbursement.Approver2Status == "Approved" &&
                     reimbursement.Approver3Status == "Pending")
            {
                reimbursement.Approver3Status = status;

                reimbursement.Approver3Remarks = remarks;

                reimbursement.Approver3ApprovedOn = DateTime.Now;
            }


            // =====================================
            // APPROVER 4
            // =====================================

            else if (reimbursement.Approver4Id == employeeId &&
                     reimbursement.Approver1Status == "Approved" &&
                     reimbursement.Approver2Status == "Approved" &&
                     reimbursement.Approver3Status == "Approved" &&
                     reimbursement.Approver4Status == "Pending")
            {
                reimbursement.Approver4Status = status;

                reimbursement.Approver4Remarks = remarks;

                reimbursement.Approver4ApprovedOn = DateTime.Now;
            }
            // =====================================
            // CEO APPROVER 
            // =====================================

            else if (isCEO &&
                  reimbursement.Approver1Status == "Approved" &&
                  reimbursement.Approver2Status == "Approved" &&
                  reimbursement.Approver3Status == "Approved" &&
                  reimbursement.Approver4Status == "Approved" &&
                  reimbursement.FinalStatus == "Pending")
            {
                reimbursement.FinalStatus = status;

                reimbursement.FinalRemarks = remarks;

                reimbursement.FinalApprovedOn = DateTime.Now;
            }

            else
            {
                TempData["error"] =
                    "You are not authorized to approve this reimbursement.";

                return RedirectToAction("Approval");
            }

            // =====================================
            // OVERALL STATUS
            // =====================================

            if (status == "Rejected")
            {
                reimbursement.Status = "Rejected";

                // FinalStatus sirf CEO rejection par update hoga
                if (isCEO)
                {
                    reimbursement.FinalStatus = "Rejected";
                }
            }
            else
            {
                // =====================================
                // CEO FINAL APPROVAL
                // =====================================

                if (isCEO &&
                    reimbursement.Approver1Status == "Approved" &&
                    reimbursement.Approver2Status == "Approved" &&
                    reimbursement.Approver3Status == "Approved" &&
                    reimbursement.Approver4Status == "Approved")
                {
                    reimbursement.Status = "Approved";
                    reimbursement.FinalStatus = "Approved";
                }

                // =====================================
                // APPROVER 4
                // =====================================

                else if (reimbursement.Approver4Id.HasValue)
                {
                    reimbursement.Status = "Pending";
                    reimbursement.FinalStatus = "Pending";
                }

                // =====================================
                // APPROVER 3
                // =====================================

                else if (reimbursement.Approver3Id.HasValue)
                {
                    reimbursement.Status =
                        reimbursement.Approver3Status == "Approved"
                            ? "Approved"
                            : "Pending";
                }

                // =====================================
                // APPROVER 2
                // =====================================

                else if (reimbursement.Approver2Id.HasValue)
                {
                    reimbursement.Status =
                        reimbursement.Approver2Status == "Approved"
                            ? "Approved"
                            : "Pending";
                }
            }

            reimbursement.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                $"Reimbursement {status.ToLower()} successfully.";

            return RedirectToAction("Approval");
        }
        private async Task<Employee?> GetCurrentEmployee()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return null;

            return await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id);
        }
        [HttpGet]
        public async Task<IActionResult> ApprovedReimbursement()
        {
            var employee = await GetCurrentEmployee();

            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int employeeId = employee.EmployeeId;
            bool isCEO = employee.DepartmentId == 22;
            var list = await _context.Reimbursements
                .Where(r => r.IsActive &&
                    (
                        (r.Approver2Id == employeeId &&
                         r.Approver2Status == "Approved")

                        ||

                        (r.Approver3Id == employeeId &&
                         r.Approver3Status == "Approved")

                        ||

                        (r.Approver4Id == employeeId &&
                         r.Approver4Status == "Approved")
                          ||

                        (isCEO && r.FinalStatus == "Approved")
                    ))
                .OrderByDescending(r => r.UpdatedOn)
                .Select(r => new ReimbursementsVM
                {
                    ReimbursementId = r.ReimbursementId,

                    EmployeeId = r.EmployeeId,

                    RequestType = r.RequestType,
                    ExpenseCategory = r.ExpenseCategory,
                    ExpenseAmount = r.ExpenseAmount,
                    ExpenseDate = r.ExpenseDate,
                    VendorName = r.VendorName,
                    PaymentMode = r.PaymentMode,
                    Purpose = r.Purpose,

                    SupportingDocument = r.SupportingDocument,
                    InvoiceBill = r.InvoiceBill,
                    FinalStatus = r.FinalStatus,
                    Status = r.Status,

                    // Logged-in user ke according remark
                    Remarks =
                        r.Approver2Id == employeeId
                            ? r.Approver2Remarks

                        : r.Approver3Id == employeeId
                            ? r.Approver3Remarks

                        : r.Approver4Id == employeeId
                            ? r.Approver4Remarks
                        : isCEO
                        ? r.FinalRemarks
                        : null,
                       

                    CreatedOn = r.CreatedOn,
                    UpdatedOn = r.UpdatedOn

                })
                .ToListAsync();

            return View(list);
        }
        [HttpGet]
        public async Task<IActionResult> RejectedReimbursement()
        {
            var employee = await GetCurrentEmployee();

            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int employeeId = employee.EmployeeId;
            bool isCEO = employee.DepartmentId == 22;
            var list = await _context.Reimbursements
                .Where(r => r.IsActive &&
                    (
                        (r.Approver2Id == employeeId &&
                         r.Approver2Status == "Rejected")

                        ||

                        (r.Approver3Id == employeeId &&
                         r.Approver3Status == "Rejected")

                        ||

                        (r.Approver4Id == employeeId &&
                         r.Approver4Status == "Rejected")
                         ||

                        // CEO FINAL REJECTION
                        (isCEO &&
                         r.FinalStatus == "Rejected")
                    ))
                .OrderByDescending(r => r.UpdatedOn)
                .Select(r => new ReimbursementsVM
                {
                    ReimbursementId = r.ReimbursementId,

                    EmployeeId = r.EmployeeId,

                    RequestType = r.RequestType,
                    ExpenseCategory = r.ExpenseCategory,
                    ExpenseAmount = r.ExpenseAmount,
                    ExpenseDate = r.ExpenseDate,
                    VendorName = r.VendorName,
                    PaymentMode = r.PaymentMode,
                    Purpose = r.Purpose,

                    SupportingDocument = r.SupportingDocument,
                    InvoiceBill = r.InvoiceBill,
                    FinalStatus = r.FinalStatus,
                    Status = r.Status,

                    // Sirf logged-in approver ka remark
                    Remarks =
                        r.Approver2Id == employeeId
                            ? r.Approver2Remarks

                        : r.Approver3Id == employeeId
                            ? r.Approver3Remarks

                        : r.Approver4Id == employeeId
                            ? r.Approver4Remarks
                        : isCEO
                        ? r.FinalRemarks      
                        : null,

                    CreatedOn = r.CreatedOn,
                    UpdatedOn = r.UpdatedOn

                })
                .ToListAsync();

            return View(list);
        }

    }
}
