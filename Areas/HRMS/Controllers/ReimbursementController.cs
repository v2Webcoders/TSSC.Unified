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
        private void LoadDropdowns(ReimbursementVM model)
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
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new ReimbursementVM();

            LoadDropdowns(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReimbursementVM model)
        {
            // ⭐⭐⭐ MOST IMPORTANT - REMOVE ALL NON-FORM FIELDS ⭐⭐⭐
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
            ModelState.Remove("RequestTypeList");
            ModelState.Remove("ExpenseCategoryList");
            ModelState.Remove("PaymentModeList");
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
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
            if (model.SupportingDocumentFile != null)
            {
                string fileName =
                    Guid.NewGuid().ToString()
                    + Path.GetExtension(model.SupportingDocumentFile.FileName);

                string filePath =
                    Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await model.SupportingDocumentFile.CopyToAsync(stream);
                }
                model.SupportingDocument =
                    "/uploads/reimbursements/" + fileName;
            }
            if (model.InvoiceBillFile != null)
            {
                string fileName =
                    Guid.NewGuid().ToString()
                    + Path.GetExtension(model.InvoiceBillFile.FileName);

                string filePath =
                    Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await model.InvoiceBillFile.CopyToAsync(stream);
                }
                model.InvoiceBill =
                    "/uploads/reimbursements/" + fileName;
            }
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                ModelState.AddModelError("", "Employee record not found for current user.");
                return View(model);
            }
            var reimbursement = new Reimbursement
            {
                EmployeeId = employee.EmployeeId,
                RequestType = model.RequestType,
                ExpenseCategory = model.ExpenseCategory,
                ExpenseAmount = model.ExpenseAmount,
                ExpenseDate = model.ExpenseDate,
                VendorName = model.VendorName,
                PaymentMode = model.PaymentMode,
                Purpose = model.Purpose,
                SupportingDocument = model.SupportingDocument,
                InvoiceBill = model.InvoiceBill,
                Remarks = model.Remarks,
                Status = "Pending Manager Approval",
                FinanceStatus = "Pending",
                ReportingManagerId = employee.ReportingManagerId,
                CreatedOn = DateTime.Now,
                IsActive = true
            };
            _context.Reimbursement.Add(reimbursement);
            await _context.SaveChangesAsync();
            TempData["msg"] =
                "Reimbursement request submitted successfully.";
            return RedirectToAction("Create");
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
      .Where(x => x.EmployeeId == employee.EmployeeId && x.IsActive)
      .OrderByDescending(x => x.CreatedOn)
      .Select(x => new ReimbursementVM
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
          CreatedOn = x.CreatedOn
      })
      .ToListAsync();

            return View(model);
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
        public async Task<IActionResult> ManagerApprove(int id, string remarks)
        {
            var reimbursement = await _context.Reimbursement
                .FirstOrDefaultAsync(x => x.ReimbursementId == id);

            if (reimbursement == null)
            {
                return NotFound();
            }

            reimbursement.Status = "Manager Approved";
            reimbursement.ManagerRemarks = remarks;
            reimbursement.ManagerApprovedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Reimbursement approved successfully.";

            return RedirectToAction("ManagerReimbursement");
        }
        [HttpPost]
        public async Task<IActionResult> ManagerReject(int id, string remarks)
        {
            var reimbursement = await _context.Reimbursement
                .FirstOrDefaultAsync(x => x.ReimbursementId == id);

            if (reimbursement == null)
            {
                return NotFound();
            }

            reimbursement.Status = "Rejected";
            reimbursement.ManagerRemarks = remarks;
            reimbursement.ManagerApprovedOn = DateTime.Now;
            await _context.SaveChangesAsync();

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
        public async Task<IActionResult> FinanceApprove(int id, string remarks)
        {
            var reimbursement = await _context.Reimbursement
                .FirstOrDefaultAsync(x =>
                    x.ReimbursementId == id &&
                    x.Status == "Manager Approved");

            if (reimbursement == null)
            {
                return NotFound();
            }

            reimbursement.FinanceStatus = "Finance Approved";
            reimbursement.FinanceRemarks = remarks;
            reimbursement.FinanceApprovedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Reimbursement approved by Finance.";

            return RedirectToAction("FinanceReimbursement");
        }
        [HttpPost]
        public async Task<IActionResult> FinanceReject(int id, string remarks)
        {
            var reimbursement = await _context.Reimbursement
                .FirstOrDefaultAsync(x =>
                    x.ReimbursementId == id &&
                    x.Status == "Manager Approved");

            if (reimbursement == null)
            {
                return NotFound();
            }

            reimbursement.FinanceStatus = "Finance Rejected";
            reimbursement.FinanceRemarks = remarks;
            reimbursement.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Reimbursement rejected by Finance.";

            return RedirectToAction("FinanceReimbursement");
        }
    }
}
