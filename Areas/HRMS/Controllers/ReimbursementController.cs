using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Areas.HRMS.Controllers;
using QUIZAPP.Models;
using System;
using System.Linq.Dynamic.Core;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;


namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class ReimbursementController:Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public ReimbursementController(ILogger<EmployeeController> logger,
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
        #region
        private void LoadDropdowns(ReimbursementVM model)
        {

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
        }
        #endregion

        [HttpGet]
        public async Task<IActionResult> Create(int? id)
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
                return NotFound();
            }

            var model = new ReimbursementVM();

            // EDIT
            if (id.HasValue && id.Value > 0)
            {
                var reimbursement = await _context.Reimbursement
                 .Where(x =>
                     x.ReimbursementId == id.Value &&
                     x.EmployeeId == employee.EmployeeId &&
                     x.IsActive)
                 .Select(x => new
                 {
                     x.ReimbursementId,
                     x.EmployeeId,
                     x.ExpenseCategory,
                     x.ExpenseAmount,
                     x.ExpenseDate,
                     x.PaymentMode,
                     x.Purpose,
                     x.SupportingDocument,
                     x.InvoiceBill
                 })
                 .FirstOrDefaultAsync();

                if (reimbursement == null)
                {
                    return NotFound();
                }

                model.ReimbursementId = reimbursement.ReimbursementId;
                model.EmployeeId = reimbursement.EmployeeId;

                model.ExpenseCategory = reimbursement.ExpenseCategory;
                model.ExpenseAmount = reimbursement.ExpenseAmount;
                model.ExpenseDate = reimbursement.ExpenseDate;
                model.PaymentMode = reimbursement.PaymentMode;
                model.Purpose = reimbursement.Purpose;

                model.SupportingDocument = reimbursement.SupportingDocument;
                model.InvoiceBill = reimbursement.InvoiceBill;
            }
            else
            {
                // NEW
                model.EmployeeId = employee.EmployeeId;
                model.ExpenseDate = DateTime.Today;
            }

            // Employee details
            model.EmployeeCode = employee.EmployeeCode;

            model.EmployeeName =
                $"{employee.FirstName} {employee.LastName}".Trim();

            // Department
            var department = await _context.Department
                .FirstOrDefaultAsync(x =>
                    x.DepartmentId == employee.DepartmentId);

            model.DepartmentName = department?.DepartmentName;

            LoadDropdowns(model);

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReimbursementVM model)
        {
            ModelState.Remove("SupportingDocument");
            ModelState.Remove("InvoiceBill");

            ModelState.Remove("Status");
            ModelState.Remove("ManagerRemarks");
            ModelState.Remove("FinanceRemarks");
            ModelState.Remove("ManagerApprovedOn");
            ModelState.Remove("FinanceApprovedOn");
            ModelState.Remove("PaidOn");
            ModelState.Remove("UpdatedOn");
            ModelState.Remove("CreatedOn");
            ModelState.Remove("IsActive");

            ModelState.Remove("ExpenseCategoryList");
            ModelState.Remove("PaymentModeList");

            ModelState.Remove("EmployeeName");
            ModelState.Remove("EmployeeCode");
            ModelState.Remove("DepartmentName");
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
                ModelState.AddModelError(
                    "",
                    "Employee record not found for current user."
                );

                LoadDropdowns(model);

                return View(model);
            }
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);

                model.EmployeeId = employee.EmployeeId;

                model.EmployeeCode = employee.EmployeeCode;

                model.EmployeeName =
                    $"{employee.FirstName} {employee.LastName}".Trim();

                var department = await _context.Department
                    .FirstOrDefaultAsync(x =>
                        x.DepartmentId == employee.DepartmentId);

                model.DepartmentName =
                    department?.DepartmentName;

                return View(model);
            }

            string uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "reimbursements"
            );

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            Reimbursement reimbursement;

            if (model.ReimbursementId > 0)
            {
                var existingData = await _context.Reimbursement
                    .Where(x =>
                        x.ReimbursementId == model.ReimbursementId &&
                        x.EmployeeId == employee.EmployeeId &&
                        x.IsActive)
                    .Select(x => new
                    {
                        x.ReimbursementId,
                        x.Status,
                        x.SupportingDocument,
                        x.InvoiceBill
                    })
                    .FirstOrDefaultAsync();

                if (existingData == null)
                {
                    return NotFound();
                }
                if (existingData.Status != "Pending Manager Approval")
                {
                    TempData["msg"] =
                        "This reimbursement can no longer be edited.";

                    return RedirectToAction("MyReimbursement");
                }

                reimbursement = new Reimbursement
                {
                    ReimbursementId = existingData.ReimbursementId,

                    SupportingDocument =
                        existingData.SupportingDocument,

                    InvoiceBill =
                        existingData.InvoiceBill
                };

                _context.Reimbursement.Attach(reimbursement);
            }
            else
            {
                reimbursement = new Reimbursement
                {
                    EmployeeId = employee.EmployeeId,

                    Status = "Pending Manager Approval",

                    FinanceStatus = "Pending",

                    ReportingManagerId =
                        employee.ReportingManagerId,

                    CreatedOn = DateTime.Now,

                    IsActive = true
                };

                _context.Reimbursement.Add(reimbursement);
            }
            if (model.SupportingDocumentFile != null)
            {
                string fileName =
                    Guid.NewGuid().ToString()
                    + Path.GetExtension(
                        model.SupportingDocumentFile.FileName
                    );

                string filePath =
                    Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await model.SupportingDocumentFile
                        .CopyToAsync(stream);
                }

                reimbursement.SupportingDocument =
                    "/uploads/reimbursements/" + fileName;
            }
            if (model.InvoiceBillFile != null)
            {
                string fileName =
                    Guid.NewGuid().ToString()
                    + Path.GetExtension(
                        model.InvoiceBillFile.FileName
                    );

                string filePath =
                    Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await model.InvoiceBillFile
                        .CopyToAsync(stream);
                }

                reimbursement.InvoiceBill =
                    "/uploads/reimbursements/" + fileName;
            }

            reimbursement.ExpenseCategory =model.ExpenseCategory;
            reimbursement.ExpenseAmount = model.ExpenseAmount;
            reimbursement.ExpenseDate = model.ExpenseDate;
            reimbursement.PaymentMode = model.PaymentMode;
            reimbursement.Purpose = model.Purpose;
            if (model.ReimbursementId > 0)
            {
                reimbursement.UpdatedOn = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["msg"] =
                model.ReimbursementId > 0
                    ? "Reimbursement updated successfully."
                    : "Reimbursement request submitted successfully.";


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
                return View(new MyReimbursementVM
                {
                    ReimbursementList = new List<ReimbursementVM>()
                });
            }

            var model = new MyReimbursementVM();

            model.ReimbursementList = await _context.Reimbursement
                .Where(x =>
                    x.EmployeeId == employee.EmployeeId &&
                    x.IsActive)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new ReimbursementVM
                {
                    ReimbursementId = x.ReimbursementId,

                    EmployeeId = x.EmployeeId,

                    ExpenseCategory = x.ExpenseCategory,

                    ExpenseAmount = x.ExpenseAmount,

                    ExpenseDate = x.ExpenseDate,

                    PaymentMode = x.PaymentMode,

                    Purpose = x.Purpose,

                    SupportingDocument = x.SupportingDocument,

                    InvoiceBill = x.InvoiceBill,

                    Status = x.Status,

                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
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
                TempData["msg"] = "Employee record not found.";
                return RedirectToAction("MyReimbursement");
            }

            // Get only required fields
            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.EmployeeId == employee.EmployeeId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] = "Reimbursement not found.";
                return RedirectToAction("MyReimbursement");
            }

            // Delete only when manager approval is pending
            if (reimbursement.Status != "Pending Manager Approval")
            {
                TempData["msg"] =
                    "You can delete the reimbursement only while manager approval is pending.";

                return RedirectToAction("MyReimbursement");
            }

            // Soft delete
            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.EmployeeId == employee.EmployeeId &&
                    x.IsActive &&
                    x.Status == "Pending Manager Approval")
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.IsActive, false)
                    .SetProperty(r => r.UpdatedOn, DateTime.Now)
                );

            TempData["msg"] = "Reimbursement deleted successfully.";

            return RedirectToAction("MyReimbursement");
        }
        [HttpGet]
        public async Task<IActionResult> ManagerReimbursement(string status = "Pending Manager Approval")
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var manager = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (manager == null)
            {
                return NotFound("Manager employee record not found.");
            }

            var list = await _context.Reimbursement
                .Where(x =>
                    x.IsActive &&
                    x.Status == status &&
                    x.ReportingManagerId == manager.EmployeeId)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new ReimbursementVM
                {
                    ReimbursementId = x.ReimbursementId,
                    EmployeeId = x.EmployeeId,
                    //RequestType = x.RequestType,
                    ExpenseCategory = x.ExpenseCategory,
                    ExpenseAmount = x.ExpenseAmount,
                    ExpenseDate = x.ExpenseDate,
                   // VendorName = x.VendorName,
                    PaymentMode = x.PaymentMode,
                    Purpose = x.Purpose,
                    SupportingDocument = x.SupportingDocument,
                    InvoiceBill = x.InvoiceBill,
                  //  Remarks = x.Remarks,
                    Status = x.Status,
                    ReportingManagerId = x.ReportingManagerId,
                    ManagerRemarks = x.ManagerRemarks,
                    ManagerApprovedOn = x.ManagerApprovedOn,
                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();

            ViewBag.Status = status;

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagerApprove(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter manager remarks.";
                return RedirectToAction("ManagerReimbursement");
            }

            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] = "Reimbursement not found.";
                return RedirectToAction("ManagerReimbursement");
            }

            if (reimbursement.Status != "Pending Manager Approval")
            {
                TempData["msg"] = "This reimbursement has already been processed.";
                return RedirectToAction("ManagerReimbursement");
            }

            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.IsActive &&
                    x.Status == "Pending Manager Approval")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Manager Approved")
                    .SetProperty(x => x.ManagerRemarks, remarks)
                    .SetProperty(x => x.ManagerApprovedOn, DateTime.Now)
                );

            TempData["msg"] = "Reimbursement approved successfully.";

            return RedirectToAction("ManagerReimbursement");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagerReject(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter manager remarks.";
                return RedirectToAction("ManagerReimbursement");
            }
            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] = "Reimbursement not found.";
                return RedirectToAction("ManagerReimbursement");
            }
            if (reimbursement.Status != "Pending Manager Approval")
            {
                TempData["msg"] =
                    "This reimbursement has already been processed.";

                return RedirectToAction("ManagerReimbursement");
            }
            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.IsActive &&
                    x.Status == "Pending Manager Approval")
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.Status, "Rejected")
                    .SetProperty(r => r.ManagerRemarks, remarks)
                    .SetProperty(r => r.ManagerApprovedOn, DateTime.Now)
                );

            TempData["msg"] = "Reimbursement rejected.";

            return RedirectToAction("ManagerReimbursement");
        }
        [HttpGet]
        public async Task<IActionResult> FinanceReimbursement(string status = "Pending")
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

            var list = await _context.Reimbursement
                .Where(x =>
                    x.IsActive &&
                    x.Status == "Manager Approved" &&
            x.FinanceStatus == status)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new ReimbursementVM
                {
                    ReimbursementId = x.ReimbursementId,
                    EmployeeId = x.EmployeeId,
                  //  RequestType = x.RequestType,
                    ExpenseCategory = x.ExpenseCategory,
                    ExpenseAmount = x.ExpenseAmount,
                    ExpenseDate = x.ExpenseDate,
                  //  VendorName = x.VendorName,
                    PaymentMode = x.PaymentMode,
                    Purpose = x.Purpose,
                    SupportingDocument = x.SupportingDocument,
                    InvoiceBill = x.InvoiceBill,
                  //  Remarks = x.Remarks,
                    Status = x.Status,
                    FinanceStatus = x.FinanceStatus,
                    ReportingManagerId = x.ReportingManagerId,
                    ManagerRemarks = x.ManagerRemarks,
                    ManagerApprovedOn = x.ManagerApprovedOn,
                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();
            ViewBag.Status = status;
            return View("FinanceReimbursement", list);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinanceApprove(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter finance remarks.";
                return RedirectToAction("FinanceReimbursement");
            }
            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.Status == "Manager Approved" &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.FinanceStatus
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] =
                    "Reimbursement not found or is not pending for finance approval.";

                return RedirectToAction("FinanceReimbursement");
            }
            if (reimbursement.FinanceStatus != "Pending")
            {
                TempData["msg"] =
                    "This reimbursement has already been processed by Finance.";

                return RedirectToAction("FinanceReimbursement");
            }
            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.Status == "Manager Approved" &&
                    x.FinanceStatus == "Pending" &&
                    x.IsActive)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.FinanceStatus, "Finance Approved")
                    .SetProperty(r => r.FinanceRemarks, remarks)
                    .SetProperty(r => r.FinanceApprovedOn, DateTime.Now)
                    .SetProperty(r => r.HRStatus, "Pending")
                );

            TempData["msg"] =
                "Reimbursement approved by Finance and sent to HR.";

            return RedirectToAction("FinanceReimbursement");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinanceReject(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter finance remarks.";
                return RedirectToAction("FinanceReimbursement");
            }
            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.Status == "Manager Approved" &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.FinanceStatus
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] =
                    "Reimbursement not found or is not pending for finance approval.";

                return RedirectToAction("FinanceReimbursement");
            }
            if (reimbursement.FinanceStatus == "Finance Approved" ||
                reimbursement.FinanceStatus == "Finance Rejected")
            {
                TempData["msg"] =
                    "This reimbursement has already been processed by Finance.";

                return RedirectToAction("FinanceReimbursement");
            }
            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.Status == "Manager Approved" &&
                    x.IsActive)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.FinanceStatus, "Finance Rejected")
                    .SetProperty(r => r.FinanceRemarks, remarks)
                    .SetProperty(r => r.UpdatedOn, DateTime.Now)
                );

            TempData["msg"] =
                "Reimbursement rejected by Finance.";

            return RedirectToAction("FinanceReimbursement");
        }

        [HttpGet]
        public async Task<IActionResult> HRReimbursement(string status = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var list = await _context.Reimbursement
                .Where(x =>
                    x.IsActive &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == status)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new ReimbursementVM
                {
                    ReimbursementId = x.ReimbursementId,
                    EmployeeId = x.EmployeeId,

                    ExpenseCategory = x.ExpenseCategory,
                    ExpenseAmount = x.ExpenseAmount,
                    ExpenseDate = x.ExpenseDate,
                    PaymentMode = x.PaymentMode,
                    Purpose = x.Purpose,

                    SupportingDocument = x.SupportingDocument,
                    InvoiceBill = x.InvoiceBill,

                    Status = x.Status,
                    FinanceStatus = x.FinanceStatus,

                    HRStatus = x.HRStatus,

                    ManagerRemarks = x.ManagerRemarks,
                    ManagerApprovedOn = x.ManagerApprovedOn,

                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();

            ViewBag.Status = status;

            return View("HRReimbursement", list);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRApprove(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter HR remarks.";
                return RedirectToAction("HRReimbursement");
            }

            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "Pending" &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.HRStatus
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] =
                    "Reimbursement not found or already processed.";

                return RedirectToAction("HRReimbursement");
            }

            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "Pending" &&
                    x.IsActive)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.HRStatus, "HR Approved")
                    .SetProperty(r => r.HRRemarks, remarks)
                    .SetProperty(r => r.HRApprovedOn, DateTime.Now)
                     .SetProperty(r => r.FinanceHeadStatus, "Pending")
                );

            TempData["msg"] =
                "Reimbursement approved by HR.";

            return RedirectToAction("HRReimbursement");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRReject(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter HR remarks.";
                return RedirectToAction("HRReimbursement");
            }

            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "Pending" &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.HRStatus
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] =
                    "Reimbursement not found or already processed.";

                return RedirectToAction("HRReimbursement");
            }

            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "Pending" &&
                    x.IsActive)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.HRStatus, "HR Rejected")
                    .SetProperty(r => r.HRRemarks, remarks)
                    .SetProperty(r => r.HRApprovedOn, DateTime.Now)
                );

            TempData["msg"] =
                "Reimbursement rejected by HR.";

            return RedirectToAction("HRReimbursement");
        }
        [HttpGet]
        public async Task<IActionResult> FinanceHeadReimbursement(string status = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var list = await _context.Reimbursement
                .Where(x =>
                    x.IsActive &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == status)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new ReimbursementVM
                {
                    ReimbursementId = x.ReimbursementId,
                    EmployeeId = x.EmployeeId,
                    ExpenseCategory = x.ExpenseCategory,
                    ExpenseAmount = x.ExpenseAmount,
                    ExpenseDate = x.ExpenseDate,
                    PaymentMode = x.PaymentMode,
                    Purpose = x.Purpose,
                    SupportingDocument = x.SupportingDocument,
                    InvoiceBill = x.InvoiceBill,

                    Status = x.Status,
                    FinanceStatus = x.FinanceStatus,
                    HRStatus = x.HRStatus,

                    FinanceHeadStatus = x.FinanceHeadStatus,
                    FinanceHeadRemarks = x.FinanceHeadRemarks,
                    FinanceHeadApprovedOn = x.FinanceHeadApprovedOn,

                    ManagerRemarks = x.ManagerRemarks,
                    ManagerApprovedOn = x.ManagerApprovedOn,
                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();

            ViewBag.Status = status;

            return View("FinanceHeadReimbursement", list);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinanceHeadApprove(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter Finance Head remarks.";
                return RedirectToAction("FinanceHeadReimbursement");
            }

            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == "Pending" &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.FinanceHeadStatus
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] =
                    "Reimbursement not found or already processed.";

                return RedirectToAction("FinanceHeadReimbursement");
            }

            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == "Pending" &&
                    x.IsActive)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.FinanceHeadStatus, "Finance Head Approved")
                    .SetProperty(r => r.FinanceHeadRemarks, remarks)
                    .SetProperty(r => r.FinanceHeadApprovedOn, DateTime.Now)
                    .SetProperty(r => r.CEOStatus, "Pending")
                );

            TempData["msg"] =
                "Reimbursement approved by Finance Head.";

            return RedirectToAction("FinanceHeadReimbursement");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinanceHeadReject(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter Finance Head remarks.";
                return RedirectToAction("FinanceHeadReimbursement");
            }

            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == "Pending" &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ReimbursementId,
                    x.FinanceHeadStatus
                })
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] =
                    "Reimbursement not found or already processed.";

                return RedirectToAction("FinanceHeadReimbursement");
            }

            await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == "Pending" &&
                    x.IsActive)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.FinanceHeadStatus, "Finance Head Rejected")
                    .SetProperty(r => r.FinanceHeadRemarks, remarks)
                    .SetProperty(r => r.FinanceHeadApprovedOn, DateTime.Now)
                );

            TempData["msg"] =
                "Reimbursement rejected by Finance Head.";

            return RedirectToAction("FinanceHeadReimbursement");
        }
        [HttpGet]
        public async Task<IActionResult> CEOReimbursement(string status = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var list = await _context.Reimbursement
                .Where(x =>
                    x.IsActive &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == "Finance Head Approved" &&
                    x.CEOStatus == status)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new ReimbursementVM
                {
                    ReimbursementId = x.ReimbursementId,
                    EmployeeId = x.EmployeeId,

                    ExpenseCategory = x.ExpenseCategory,
                    ExpenseAmount = x.ExpenseAmount,
                    ExpenseDate = x.ExpenseDate,
                    PaymentMode = x.PaymentMode,
                    Purpose = x.Purpose,

                    SupportingDocument = x.SupportingDocument,
                    InvoiceBill = x.InvoiceBill,

                    Status = x.Status,
                    FinanceStatus = x.FinanceStatus,
                    HRStatus = x.HRStatus,
                    FinanceHeadStatus = x.FinanceHeadStatus,

                    CEOStatus = x.CEOStatus,
                    CEORemarks = x.CEORemarks,
                    CEOApprovedOn = x.CEOApprovedOn,

                    ManagerRemarks = x.ManagerRemarks,
                    ManagerApprovedOn = x.ManagerApprovedOn,

                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();

            ViewBag.Status = status;

            return View("CEOReimbursement", list);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CEOApprove(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter CEO remarks.";
                return RedirectToAction("CEOReimbursement");
            }

            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == "Finance Head Approved" &&
                    x.CEOStatus == "Pending" &&
                    x.IsActive)
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] =
                    "Reimbursement not found or already processed.";

                return RedirectToAction("CEOReimbursement");
            }

            reimbursement.CEOStatus = "CEO Approved";
            reimbursement.CEORemarks = remarks;
            reimbursement.CEOApprovedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Reimbursement approved by CEO.";

            return RedirectToAction("CEOReimbursement");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CEOReject(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["msg"] = "Please enter CEO remarks.";
                return RedirectToAction("CEOReimbursement");
            }

            var reimbursement = await _context.Reimbursement
                .Where(x =>
                    x.ReimbursementId == id &&
                    x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == "Finance Head Approved" &&
                    x.CEOStatus == "Pending" &&
                    x.IsActive)
                .FirstOrDefaultAsync();

            if (reimbursement == null)
            {
                TempData["msg"] =
                    "Reimbursement not found or already processed.";

                return RedirectToAction("CEOReimbursement");
            }

            reimbursement.CEOStatus = "CEO Rejected";
            reimbursement.CEORemarks = remarks;
            reimbursement.CEOApprovedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] =
                "Reimbursement rejected by CEO.";

            return RedirectToAction("CEOReimbursement");
        }
    }
}
