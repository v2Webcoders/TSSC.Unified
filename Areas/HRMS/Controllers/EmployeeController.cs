using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP.ViewModel;
using System;
using System.Diagnostics;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QUIZAPP.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "HR")]
    public class EmployeeController : Controller
    {
        

        private readonly ILogger<EmployeeController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        public EmployeeController(
            ILogger<EmployeeController> logger,
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
        [HttpGet]
        public async Task<IActionResult> Create(int? id)
        {
            await LoadDropDowns();

            if (id == null)
                return View(new EmployeeVM());
            var employee = await _context.Employee.FindAsync(id);

            if (employee == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(employee.ApplicationUserId);

            var model = new EmployeeVM
            {
                EmployeeId = employee.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Gender = employee.Gender,
                DOB = employee.DOB,
                BloodGroup = employee.BloodGroup,
                MaritalStatus = employee.MaritalStatus,

                DepartmentId = employee.DepartmentId,
                DesignationId = employee.DesignationId,
                ReportingManagerId = employee.ReportingManagerId,
                JoiningDate = employee.JoiningDate,
                EmployeeType = employee.EmployeeType,
                EmploymentStatus = employee.EmploymentStatus,

                OfficialEmail = employee.OfficialEmail,
                PersonalEmail = employee.PersonalEmail,
                MobileNo = employee.MobileNo,
                AlternateMobile = employee.AlternateMobile,

                CurrentAddress = employee.CurrentAddress,
                PermanentAddress = employee.PermanentAddress,

                AadhaarNo = employee.AadhaarNo,
                PANNo = employee.PANNo,
                PassportNo = employee.PassportNo,
                UANNo = employee.UANNo,
                PFNo = employee.PFNo,
                ESICNo = employee.ESICNo,

                BankName = employee.BankName,
                AccountNo = employee.AccountNo,
                IFSCCode = employee.IFSCCode,

                CTC = employee.CTC,
                BasicSalary = employee.BasicSalary,
                HRA = employee.HRA,
                SpecialAllowance = employee.SpecialAllowance,
                PhotoPath = employee.PhotoPath,   // <-- Added
                Username = user?.UserName,
                IsActive = employee.IsActive
            };
            

            // Get current role of user
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Any())
                {
                    string roleName = roles.First();

                    var role = await _roleManager.FindByNameAsync(roleName);

                    if (role != null)
                    {
                        model.RoleId = role.Id;   // if RoleId is string
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeVM model)
        {
            await LoadDropDowns();

            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    if (item.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"Field: {item.Key}");

                        foreach (var error in item.Value.Errors)
                        {
                            Console.WriteLine($"Error: {error.ErrorMessage}");
                        }
                    }
                }

                return View(model);
            }

            string? photoPath = null;

            if (model.Photo != null && model.Photo.Length > 0)
            {
                string folder = Path.Combine(_environment.WebRootPath, "uploads", "employees");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.Photo.FileName);

                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(stream);
                }

                photoPath = "/uploads/employees/" + fileName;
            }
            //===========================
            // CREATE
            //===========================

            if (model.EmployeeId == 0)
            {
                if (await _userManager.FindByNameAsync(model.Username) != null)
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    return View(model);
                }

                var appUser = new AppUser
                {
                    UserName = model.Username,
                    Email = model.OfficialEmail,
                    PhoneNumber = model.MobileNo,
                    EmailConfirmed = true
                };

                if (string.IsNullOrWhiteSpace(model.Username) ||
    string.IsNullOrWhiteSpace(model.Password) ||
    string.IsNullOrWhiteSpace(model.RoleId))
                {
                    ModelState.AddModelError("", "Username, Password and Role are required.");

                    return View(model);
                }

                var result = await _userManager.CreateAsync(appUser, model.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);

                        Console.WriteLine($"Code: {error.Code}");
                        Console.WriteLine($"Description: {error.Description}");
                    }

                    return View(model);
                }

                var role = await _context.Roles.FindAsync(model.RoleId);

                if (role != null)
                    await _userManager.AddToRoleAsync(appUser, role.Name);

                var employee = new Employee
                {
                    ApplicationUserId = appUser.Id,

                    EmployeeCode = model.EmployeeCode,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Gender = model.Gender,
                    DOB = model.DOB,
                    BloodGroup = model.BloodGroup,
                    MaritalStatus = model.MaritalStatus,

                    DepartmentId = model.DepartmentId.Value,
                    DesignationId = model.DesignationId.Value,
                    ReportingManagerId = model.ReportingManagerId,
                    JoiningDate = model.JoiningDate,
                    EmployeeType = model.EmployeeType,
                    EmploymentStatus = model.EmploymentStatus,

                    OfficialEmail = model.OfficialEmail,
                    PersonalEmail = model.PersonalEmail,
                    MobileNo = model.MobileNo,
                    AlternateMobile = model.AlternateMobile,

                    CurrentAddress = model.CurrentAddress,
                    PermanentAddress = model.PermanentAddress,

                    AadhaarNo = model.AadhaarNo,
                    PANNo = model.PANNo,
                    PassportNo = model.PassportNo,
                    UANNo = model.UANNo,
                    PFNo = model.PFNo,
                    ESICNo = model.ESICNo,

                    BankName = model.BankName,
                    AccountNo = model.AccountNo,
                    IFSCCode = model.IFSCCode,

                    CTC = model.CTC,
                    BasicSalary = model.BasicSalary,
                    HRA = model.HRA,
                    SpecialAllowance = model.SpecialAllowance,

                    IsActive = model.IsActive,

                    CreatedBy = User.Identity!.Name,
                    CreatedDate = DateTime.Now,
                    PhotoPath = photoPath,
                };

                _context.Employee.Add(employee);
                await _context.SaveChangesAsync();

                TempData["msg"] = "Employee created successfully.";

                return RedirectToAction(nameof(EmployeeList));
            }

            //===========================
            // UPDATE
            //===========================

            var emp = await _context.Employee.FindAsync(model.EmployeeId);

            if (emp == null)
                return NotFound();

            emp.EmployeeCode = model.EmployeeCode;
            emp.FirstName = model.FirstName;
            emp.LastName = model.LastName;
            emp.Gender = model.Gender;
            emp.DOB = model.DOB;
            emp.BloodGroup = model.BloodGroup;
            emp.MaritalStatus = model.MaritalStatus;

            emp.DepartmentId = model.DepartmentId.Value;
            emp.DesignationId = model.DesignationId.Value ;
            emp.ReportingManagerId = model.ReportingManagerId;
            emp.JoiningDate = model.JoiningDate;
            emp.EmployeeType = model.EmployeeType;
            emp.EmploymentStatus = model.EmploymentStatus;

            emp.OfficialEmail = model.OfficialEmail;
            emp.PersonalEmail = model.PersonalEmail;
            emp.MobileNo = model.MobileNo;
            emp.AlternateMobile = model.AlternateMobile;

            emp.CurrentAddress = model.CurrentAddress;
            emp.PermanentAddress = model.PermanentAddress;

            emp.AadhaarNo = model.AadhaarNo;
            emp.PANNo = model.PANNo;
            emp.PassportNo = model.PassportNo;
            emp.UANNo = model.UANNo;
            emp.PFNo = model.PFNo;
            emp.ESICNo = model.ESICNo;

            emp.BankName = model.BankName;
            emp.AccountNo = model.AccountNo;
            emp.IFSCCode = model.IFSCCode;

            emp.CTC = model.CTC;
            emp.BasicSalary = model.BasicSalary;
            emp.HRA = model.HRA;
            emp.SpecialAllowance = model.SpecialAllowance;

            emp.IsActive = model.IsActive;
            emp.ModifiedBy = User.Identity!.Name;
            emp.ModifiedDate = DateTime.Now;
            if (!string.IsNullOrEmpty(photoPath))
            {
                emp.PhotoPath = photoPath;
            }
            _context.Employee.Update(emp);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Employee updated successfully.";

            return RedirectToAction(nameof(EmployeeList));
        }
        private async Task LoadDropDowns()
        {
            ViewBag.Departments = new SelectList(
                await _context.Department
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DepartmentName)
                    .ToListAsync(),
                "DepartmentId",
                "DepartmentName");

            ViewBag.Designations = new SelectList(
                await _context.Designation
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DesignationName)
                    .ToListAsync(),
                "DesignationId",
                "DesignationName");

            ViewBag.Managers = new SelectList(
                await _context.Employee
                    .OrderBy(x => x.FirstName)
                    .ToListAsync(),
                "EmployeeId",
                "FirstName");

            ViewBag.Roles = new SelectList(
                await _context.Roles.Where(x=>x.Name!="Admin").OrderBy(x => x.Name).ToListAsync(),
                "Id",
                "Name");
        }
        public async Task<IActionResult> EmployeeList()
        {
            var employees = await (
     from e in _context.Employee
     join d in _context.Department on e.DepartmentId equals d.DepartmentId
     join des in _context.Designation on e.DesignationId equals des.DesignationId
     select new EmployeeVM
     {
         EmployeeId = e.EmployeeId,
         EmployeeCode = e.EmployeeCode,
         FirstName = e.FirstName,
         LastName = e.LastName,
         MobileNo = e.MobileNo,
         OfficialEmail = e.OfficialEmail,
         DepartmentName = d.DepartmentName,
         DesignationName = des.DesignationName,
         IsActive = e.IsActive
     }).ToListAsync();

            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> ViewEmployee(int id)
        {
            var employee = (from e in _context.Employee
                            join d in _context.Department on e.DepartmentId equals d.DepartmentId
                            join des in _context.Designation on e.DesignationId equals des.DesignationId
                            where e.EmployeeId == id
                            select new EmployeeVM
                            {
                                EmployeeId = e.EmployeeId,
                                ApplicationUserId = e.ApplicationUserId,

                                EmployeeCode = e.EmployeeCode,
                                FirstName = e.FirstName,
                                LastName = e.LastName,
                                Gender = e.Gender,
                                DOB = e.DOB,
                                BloodGroup = e.BloodGroup,
                                MaritalStatus = e.MaritalStatus,

                                DepartmentId = e.DepartmentId,
                                DepartmentName = d.DepartmentName,

                                DesignationId = e.DesignationId,
                                DesignationName = des.DesignationName,

                                ReportingManagerId = e.ReportingManagerId,
                                JoiningDate = e.JoiningDate,
                                EmployeeType = e.EmployeeType,
                                EmploymentStatus = e.EmploymentStatus,

                                OfficialEmail = e.OfficialEmail,
                                PersonalEmail = e.PersonalEmail,
                                MobileNo = e.MobileNo,
                                AlternateMobile = e.AlternateMobile,

                                CurrentAddress = e.CurrentAddress,
                                PermanentAddress = e.PermanentAddress,

                                AadhaarNo = e.AadhaarNo,
                                PANNo = e.PANNo,
                                PassportNo = e.PassportNo,
                                UANNo = e.UANNo,
                                PFNo = e.PFNo,
                                ESICNo = e.ESICNo,

                                BankName = e.BankName,
                                AccountNo = e.AccountNo,
                                IFSCCode = e.IFSCCode,

                                CTC = e.CTC,
                                BasicSalary = e.BasicSalary,
                                HRA = e.HRA,
                                SpecialAllowance = e.SpecialAllowance,
                                PhotoPath = e.PhotoPath,   // <-- Added
                                Username = e.ApplicationUser.UserName,
                                IsActive = e.IsActive
                            }).FirstOrDefault();

            if (employee == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(employee.ApplicationUserId);

            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Any())
                {
                    var role = await _roleManager.FindByNameAsync(roles.First());

                    if (role != null)
                    {
                        employee.RoleName = role.Name;   // RoleId should be string
                    }
                }
            }

            return View(employee);
        }
    }
}
    

