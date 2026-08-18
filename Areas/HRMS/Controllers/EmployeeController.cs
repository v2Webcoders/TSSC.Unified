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
using System.Text.Json;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QUIZAPP.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
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
        [Authorize(Roles = "HR")]
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
                // Official Information
                Prefix = employee.Prefix,
                BranchId = employee.BranchId,
                SubBranchId = employee.SubBranchId,
                NoticePeriod = employee.NoticePeriod,

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

    //    [HttpPost]
    //    [ValidateAntiForgeryToken]
    //    public async Task<IActionResult> Create(EmployeeVM model)
    //    {
    //        await LoadDropDowns();

    //        if (!ModelState.IsValid)
    //        {
    //            foreach (var item in ModelState)
    //            {
    //                if (item.Value.Errors.Count > 0)
    //                {
    //                    Console.WriteLine($"Field: {item.Key}");

    //                    foreach (var error in item.Value.Errors)
    //                    {
    //                        Console.WriteLine($"Error: {error.ErrorMessage}");
    //                    }
    //                }
    //            }

    //            return View(model);
    //        }

    //        string? photoPath = null;

    //        if (model.Photo != null && model.Photo.Length > 0)
    //        {
    //            string folder = Path.Combine(_environment.WebRootPath, "uploads", "employees");

    //            if (!Directory.Exists(folder))
    //                Directory.CreateDirectory(folder);

    //            string fileName = Guid.NewGuid() + Path.GetExtension(model.Photo.FileName);

    //            string filePath = Path.Combine(folder, fileName);

    //            using (var stream = new FileStream(filePath, FileMode.Create))
    //            {
    //                await model.Photo.CopyToAsync(stream);
    //            }

    //            photoPath = "/uploads/employees/" + fileName;
    //        }
    //        //===========================
    //        // CREATE
    //        //===========================

    //        if (model.EmployeeId == 0)
    //        {
    //            if (await _userManager.FindByNameAsync(model.Username) != null)
    //            {
    //                ModelState.AddModelError("Username", "Username already exists.");
    //                return View(model);
    //            }

    //            var appUser = new AppUser
    //            {
    //                UserName = model.Username,
    //                Email = model.OfficialEmail,
    //                PhoneNumber = model.MobileNo,
    //                EmailConfirmed = true
    //            };

    //            if (string.IsNullOrWhiteSpace(model.Username) ||
    //string.IsNullOrWhiteSpace(model.Password) ||
    //string.IsNullOrWhiteSpace(model.RoleId))
    //            {
    //                ModelState.AddModelError("", "Username, Password and Role are required.");

    //                return View(model);
    //            }

    //            var result = await _userManager.CreateAsync(appUser, model.Password);

    //            if (!result.Succeeded)
    //            {
    //                foreach (var error in result.Errors)
    //                {
    //                    ModelState.AddModelError(error.Code, error.Description);

    //                    Console.WriteLine($"Code: {error.Code}");
    //                    Console.WriteLine($"Description: {error.Description}");
    //                }

    //                return View(model);
    //            }

    //            var role = await _context.Roles.FindAsync(model.RoleId);

    //            if (role != null)
    //                await _userManager.AddToRoleAsync(appUser, role.Name);

    //            var employee = new Employee
    //            {
    //                ApplicationUserId = appUser.Id,

    //                EmployeeCode = model.EmployeeCode,
    //                FirstName = model.FirstName,
    //                LastName = model.LastName,
    //                Gender = model.Gender,
    //                DOB = model.DOB,
    //                BloodGroup = model.BloodGroup,
    //                MaritalStatus = model.MaritalStatus,

    //                DepartmentId = model.DepartmentId.Value,
    //                DesignationId = model.DesignationId.Value,
    //                ReportingManagerId = model.ReportingManagerId,
    //                JoiningDate = model.JoiningDate,
    //                EmployeeType = model.EmployeeType,
    //                EmploymentStatus = model.EmploymentStatus,

    //                OfficialEmail = model.OfficialEmail,
    //                PersonalEmail = model.PersonalEmail,
    //                MobileNo = model.MobileNo,
    //                AlternateMobile = model.AlternateMobile,

    //                CurrentAddress = model.CurrentAddress,
    //                PermanentAddress = model.PermanentAddress,

    //                AadhaarNo = model.AadhaarNo,
    //                PANNo = model.PANNo,
    //                PassportNo = model.PassportNo,
    //                UANNo = model.UANNo,
    //                PFNo = model.PFNo,
    //                ESICNo = model.ESICNo,

    //                BankName = model.BankName,
    //                AccountNo = model.AccountNo,
    //                IFSCCode = model.IFSCCode,

    //                CTC = model.CTC,
    //                BasicSalary = model.BasicSalary,
    //                HRA = model.HRA,
    //                SpecialAllowance = model.SpecialAllowance,

    //                IsActive = model.IsActive,

    //                CreatedBy = User.Identity!.Name,
    //                CreatedDate = DateTime.Now,
    //                PhotoPath = photoPath,
    //            };

    //            _context.Employee.Add(employee);
    //            await _context.SaveChangesAsync();

    //            TempData["msg"] = "Employee created successfully.";

    //            return RedirectToAction(nameof(EmployeeList));
    //        }

    //        //===========================
    //        // UPDATE
    //        //===========================

    //        var emp = await _context.Employee.FindAsync(model.EmployeeId);

    //        if (emp == null)
    //            return NotFound();

    //        emp.EmployeeCode = model.EmployeeCode;
    //        emp.FirstName = model.FirstName;
    //        emp.LastName = model.LastName;
    //        emp.Gender = model.Gender;
    //        emp.DOB = model.DOB;
    //        emp.BloodGroup = model.BloodGroup;
    //        emp.MaritalStatus = model.MaritalStatus;

    //        emp.DepartmentId = model.DepartmentId.Value;
    //        emp.DesignationId = model.DesignationId.Value ;
    //        emp.ReportingManagerId = model.ReportingManagerId;
    //        emp.JoiningDate = model.JoiningDate;
    //        emp.EmployeeType = model.EmployeeType;
    //        emp.EmploymentStatus = model.EmploymentStatus;

    //        emp.OfficialEmail = model.OfficialEmail;
    //        emp.PersonalEmail = model.PersonalEmail;
    //        emp.MobileNo = model.MobileNo;
    //        emp.AlternateMobile = model.AlternateMobile;

    //        emp.CurrentAddress = model.CurrentAddress;
    //        emp.PermanentAddress = model.PermanentAddress;

    //        emp.AadhaarNo = model.AadhaarNo;
    //        emp.PANNo = model.PANNo;
    //        emp.PassportNo = model.PassportNo;
    //        emp.UANNo = model.UANNo;
    //        emp.PFNo = model.PFNo;
    //        emp.ESICNo = model.ESICNo;

    //        emp.BankName = model.BankName;
    //        emp.AccountNo = model.AccountNo;
    //        emp.IFSCCode = model.IFSCCode;

    //        emp.CTC = model.CTC;
    //        emp.BasicSalary = model.BasicSalary;
    //        emp.HRA = model.HRA;
    //        emp.SpecialAllowance = model.SpecialAllowance;

    //        emp.IsActive = model.IsActive;
    //        emp.ModifiedBy = User.Identity!.Name;
    //        emp.ModifiedDate = DateTime.Now;
    //        if (!string.IsNullOrEmpty(photoPath))
    //        {
    //            emp.PhotoPath = photoPath;
    //        }
    //        _context.Employee.Update(emp);
    //        await _context.SaveChangesAsync();

    //        TempData["msg"] = "Employee updated successfully.";

    //        return RedirectToAction(nameof(EmployeeList));
    //    }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
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
                using var transaction = await _context.Database.BeginTransactionAsync();

                AppUser? appUser = null;

                try
                {
                    if (await _userManager.FindByNameAsync(model.Username) != null)
                    {
                        ModelState.AddModelError("Username", "Username already exists.");
                        return View(model);
                    }

                    if (string.IsNullOrWhiteSpace(model.Username) ||
                        string.IsNullOrWhiteSpace(model.Password) ||
                        string.IsNullOrWhiteSpace(model.RoleId))
                    {
                        ModelState.AddModelError("", "Username, Password and Role are required.");
                        return View(model);
                    }

                    appUser = new AppUser
                    {
                        UserName = model.Username,
                        Email = model.OfficialEmail,
                        PhoneNumber = model.MobileNo,
                        EmailConfirmed = true,
                        Password=model.Password
                    };

                    var result = await _userManager.CreateAsync(appUser, model.Password);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError(error.Code, error.Description);

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

                        Prefix = model.Prefix,
                        BranchId = model.BranchId,
                        SubBranchId = model.SubBranchId,
                        NoticePeriod = model.NoticePeriod,

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
                        PhotoPath = photoPath
                    };

                    _context.Employee.Add(employee);

                    await _context.SaveChangesAsync();

                    // Initialize Leave Balance from active Leave Policies
                    var leavePolicies = await _context.LeavePolicy
                        .Where(x => x.IsActive
                            && x.EffectiveFrom <= DateTime.Today
                            && (x.EffectiveTo == null ||
                                x.EffectiveTo >= DateTime.Today))
                        .ToListAsync();

                    foreach (var policy in leavePolicies)
                    {
                        var leaveBalance = new EmployeeLeaveBalance
                        {
                            EmployeeId = employee.EmployeeId,
                            LeaveTypeId = policy.LeaveTypeId,
                            OpeningBalance = policy.NoOfDays,
                            UsedLeaves = 0,
                            Adjustment = 0,
                            LastUpdated = DateTime.Now
                        };

                        _context.EmployeeLeaveBalance.Add(leaveBalance);
                    }

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["msg"] = "Employee created successfully.";

                    return RedirectToAction(nameof(EmployeeList));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    // Remove Identity user if it was already created
                    if (appUser != null)
                    {
                        var user = await _userManager.FindByIdAsync(appUser.Id);

                        if (user != null)
                            await _userManager.DeleteAsync(user);
                    }

                    ModelState.AddModelError("", ex.Message);

                    return View(model);
                }
            }

            //===========================
            // UPDATE
            //===========================

            var emp = await _context.Employee.FindAsync(model.EmployeeId);

            if (emp == null)
                return NotFound();
            // New Fields
            emp.Prefix = model.Prefix;
            emp.BranchId = model.BranchId;
            emp.SubBranchId = model.SubBranchId;
            emp.NoticePeriod = model.NoticePeriod;
            emp.EmployeeCode = model.EmployeeCode;
            emp.FirstName = model.FirstName;
            emp.LastName = model.LastName;
            emp.Gender = model.Gender;
            emp.DOB = model.DOB;
            emp.BloodGroup = model.BloodGroup;
            emp.MaritalStatus = model.MaritalStatus;

            emp.DepartmentId = model.DepartmentId.Value;
            emp.DesignationId = model.DesignationId.Value;
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
            ViewBag.Branches = new SelectList(
        await _context.Branch
            .Where(x => x.IsActive)
            .OrderBy(x => x.BranchName)
            .ToListAsync(),
        "BranchId",
        "BranchName");

            ViewBag.SubBranches = new SelectList(
                await _context.SubBranch
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SubBranchName)
                    .ToListAsync(),
                "SubBranchId",
                "SubBranchName");
        }

        [Authorize(Roles = "HR")]
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
         IsActive = e.IsActive,
         Username=e.ApplicationUser.UserName
     }).ToListAsync();

            return View(employees);
        }

        //[HttpGet]
        //public async Task<IActionResult> ViewEmployee(int id)
        //{
        //    var employee = (from e in _context.Employee
        //                    join d in _context.Department on e.DepartmentId equals d.DepartmentId
        //                    join des in _context.Designation on e.DesignationId equals des.DesignationId
        //                    where e.EmployeeId == id
        //                    select new EmployeeVM
        //                    {
        //                        EmployeeId = e.EmployeeId,
        //                        ApplicationUserId = e.ApplicationUserId,

        //                        EmployeeCode = e.EmployeeCode,
        //                        FirstName = e.FirstName,
        //                        LastName = e.LastName,
        //                        Gender = e.Gender,
        //                        DOB = e.DOB,
        //                        BloodGroup = e.BloodGroup,
        //                        MaritalStatus = e.MaritalStatus,

        //                        DepartmentId = e.DepartmentId,
        //                        DepartmentName = d.DepartmentName,

        //                        DesignationId = e.DesignationId,
        //                        DesignationName = des.DesignationName,

        //                        ReportingManagerId = e.ReportingManagerId,
        //                        JoiningDate = e.JoiningDate,
        //                        EmployeeType = e.EmployeeType,
        //                        EmploymentStatus = e.EmploymentStatus,

        //                        OfficialEmail = e.OfficialEmail,
        //                        PersonalEmail = e.PersonalEmail,
        //                        MobileNo = e.MobileNo,
        //                        AlternateMobile = e.AlternateMobile,

        //                        CurrentAddress = e.CurrentAddress,
        //                        PermanentAddress = e.PermanentAddress,

        //                        AadhaarNo = e.AadhaarNo,
        //                        PANNo = e.PANNo,
        //                        PassportNo = e.PassportNo,
        //                        UANNo = e.UANNo,
        //                        PFNo = e.PFNo,
        //                        ESICNo = e.ESICNo,

        //                        BankName = e.BankName,
        //                        AccountNo = e.AccountNo,
        //                        IFSCCode = e.IFSCCode,

        //                        CTC = e.CTC,
        //                        BasicSalary = e.BasicSalary,
        //                        HRA = e.HRA,
        //                        SpecialAllowance = e.SpecialAllowance,
        //                        PhotoPath = e.PhotoPath,   // <-- Added
        //                        Username = e.ApplicationUser.UserName,
        //                        IsActive = e.IsActive
        //                    }).FirstOrDefault();

        //    if (employee == null)
        //        return NotFound();

        //    var user = await _userManager.FindByIdAsync(employee.ApplicationUserId);

        //    if (user != null)
        //    {
        //        var roles = await _userManager.GetRolesAsync(user);

        //        if (roles.Any())
        //        {
        //            var role = await _roleManager.FindByNameAsync(roles.First());

        //            if (role != null)
        //            {
        //                employee.RoleName = role.Name;   // RoleId should be string
        //            }
        //        }
        //    }

        //    return View(employee);
        //}

        [HttpGet]
        public async Task<IActionResult> ViewEmployee(int id)
        {
            var employee = (from e in _context.Employee
                            join d in _context.Department on e.DepartmentId equals d.DepartmentId
                            join des in _context.Designation on e.DesignationId equals des.DesignationId

                            join b in _context.Branch
                                on e.BranchId equals b.BranchId into branchJoin
                            from b in branchJoin.DefaultIfEmpty()

                            join sb in _context.SubBranch
                                on e.SubBranchId equals sb.SubBranchId into subBranchJoin
                            from sb in subBranchJoin.DefaultIfEmpty()

                            where e.EmployeeId == id
                            select new EmployeeVM
                            {
                                EmployeeId = e.EmployeeId,
                                ApplicationUserId = e.ApplicationUserId,

                                // Basic Information
                                EmployeeCode = e.EmployeeCode,
                                FirstName = e.FirstName,
                                LastName = e.LastName,
                                Gender = e.Gender,
                                DOB = e.DOB,
                                BloodGroup = e.BloodGroup,
                                MaritalStatus = e.MaritalStatus,

                                // Official Information
                                Prefix = e.Prefix,

                                DepartmentId = e.DepartmentId,
                                DepartmentName = d.DepartmentName,

                                DesignationId = e.DesignationId,
                                DesignationName = des.DesignationName,

                                BranchId = e.BranchId,
                                BranchName = b != null ? b.BranchName : "",

                                SubBranchId = e.SubBranchId,
                                SubBranchName = sb != null ? sb.SubBranchName : "",

                                NoticePeriod = e.NoticePeriod,

                                ReportingManagerId = e.ReportingManagerId,
                                JoiningDate = e.JoiningDate,
                                EmployeeType = e.EmployeeType,
                                EmploymentStatus = e.EmploymentStatus,

                                // Contact
                                OfficialEmail = e.OfficialEmail,
                                PersonalEmail = e.PersonalEmail,
                                MobileNo = e.MobileNo,
                                AlternateMobile = e.AlternateMobile,

                                // Address
                                CurrentAddress = e.CurrentAddress,
                                PermanentAddress = e.PermanentAddress,

                                // Identity
                                AadhaarNo = e.AadhaarNo,
                                PANNo = e.PANNo,
                                PassportNo = e.PassportNo,
                                UANNo = e.UANNo,
                                PFNo = e.PFNo,
                                ESICNo = e.ESICNo,

                                // Bank
                                BankName = e.BankName,
                                AccountNo = e.AccountNo,
                                IFSCCode = e.IFSCCode,

                                // Salary
                                CTC = e.CTC,
                                BasicSalary = e.BasicSalary,
                                HRA = e.HRA,
                                SpecialAllowance = e.SpecialAllowance,

                                // Login
                                PhotoPath = e.PhotoPath,
                                Username = e.ApplicationUser.UserName,

                                // Status
                                IsActive = e.IsActive
                            }).FirstOrDefault();

            if (employee == null)
                return NotFound();

            // Reporting Manager Name
            if (employee.ReportingManagerId.HasValue)
            {
                employee.ReportingManagerName = await _context.Employee
                    .Where(x => x.EmployeeId == employee.ReportingManagerId)
                    .Select(x => x.FirstName + " " + x.LastName)
                    .FirstOrDefaultAsync();
            }

            // Role Name
            var user = await _userManager.FindByIdAsync(employee.ApplicationUserId);

            if (user != null)
            {
                employee.Password = user.Password;
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Any())
                {
                    employee.RoleName = roles.First();
                }
            }

            return View(employee);
        }

        public class ResetPasswordRequest
        {
            public int EmployeeId { get; set; }
            public string NewPassword { get; set; } = string.Empty;
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            {
                return Json(new { success = false, message = "Password must be at least 8 characters." });
            }

            var employee = await _context.Employee.FindAsync(request.EmployeeId);
            if (employee == null || string.IsNullOrEmpty(employee.ApplicationUserId))
            {
                return Json(new { success = false, message = "Employee login not found." });
            }

            var user = await _userManager.FindByIdAsync(employee.ApplicationUserId);
            if (user == null)
            {
                return Json(new { success = false, message = "User account not found." });
            }

            // Admin-initiated reset — no old password required.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = errors });
            }

            // Optional: audit log
            // _logger.LogInformation("Password reset for employee {EmployeeId} by {AdminUser}", request.EmployeeId, User.Identity.Name);

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            try
            {
                var currentUser =
                    await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Unauthorized();

                var employee =
                    await _context.Employee
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                    return NotFound("Employee profile not found.");

                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading profile for current user.");

                return StatusCode(
                    500,
                    "Unable to load your profile. Please try again later.");
            }
        }
        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var employee = await _context.Employee
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .Include(x => x.ReportingManager)
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return NotFound("Employee profile not found.");

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
                DepartmentName = employee.Department?.DepartmentName,

                DesignationId = employee.DesignationId,
                DesignationName = employee.Designation?.DesignationName,

                ReportingManagerId = employee.ReportingManagerId,
                ReportingManagerName =
                    employee.ReportingManager != null
                        ? $"{employee.ReportingManager.FirstName} {employee.ReportingManager.LastName}".Trim()
                        : "Not assigned",

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

                IsActive = employee.IsActive,

                Prefix = employee.Prefix,

                BranchId = employee.BranchId,
                SubBranchId = employee.SubBranchId,

                NoticePeriod = employee.NoticePeriod,


                ApplicationUserId = employee.ApplicationUserId,
                PhotoPath = employee.PhotoPath
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(EmployeeVM model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return NotFound("Employee profile not found.");


            // =====================================================
            // REMOVE VALIDATION FOR HR-CONTROLLED FIELDS
            // =====================================================

            ModelState.Remove(nameof(EmployeeVM.EmployeeCode));

            ModelState.Remove(nameof(EmployeeVM.DepartmentId));
            ModelState.Remove(nameof(EmployeeVM.DesignationId));
            ModelState.Remove(nameof(EmployeeVM.ReportingManagerId));

            ModelState.Remove(nameof(EmployeeVM.JoiningDate));
            ModelState.Remove(nameof(EmployeeVM.EmployeeType));
            ModelState.Remove(nameof(EmployeeVM.EmploymentStatus));

            ModelState.Remove(nameof(EmployeeVM.OfficialEmail));

            ModelState.Remove(nameof(EmployeeVM.AadhaarNo));
            ModelState.Remove(nameof(EmployeeVM.PANNo));
            ModelState.Remove(nameof(EmployeeVM.PassportNo));
            ModelState.Remove(nameof(EmployeeVM.UANNo));
            ModelState.Remove(nameof(EmployeeVM.PFNo));
            ModelState.Remove(nameof(EmployeeVM.ESICNo));

            ModelState.Remove(nameof(EmployeeVM.BankName));
            ModelState.Remove(nameof(EmployeeVM.AccountNo));
            ModelState.Remove(nameof(EmployeeVM.IFSCCode));

            ModelState.Remove(nameof(EmployeeVM.CTC));
            ModelState.Remove(nameof(EmployeeVM.BasicSalary));
            ModelState.Remove(nameof(EmployeeVM.HRA));
            ModelState.Remove(nameof(EmployeeVM.SpecialAllowance));

            ModelState.Remove(nameof(EmployeeVM.Username));
            ModelState.Remove(nameof(EmployeeVM.Password));
            ModelState.Remove(nameof(EmployeeVM.RoleId));

            ModelState.Remove(nameof(EmployeeVM.IsActive));

            ModelState.Remove(nameof(EmployeeVM.Prefix));

            ModelState.Remove(nameof(EmployeeVM.BranchId));
            ModelState.Remove(nameof(EmployeeVM.SubBranchId));

            ModelState.Remove(nameof(EmployeeVM.NoticePeriod));

            ModelState.Remove(nameof(EmployeeVM.ApplicationUserId));

            ModelState.Remove(nameof(EmployeeVM.DepartmentName));
            ModelState.Remove(nameof(EmployeeVM.DesignationName));
            ModelState.Remove(nameof(EmployeeVM.ReportingManagerName));
            ModelState.Remove(nameof(EmployeeVM.RoleName));
            ModelState.Remove(nameof(EmployeeVM.BranchName));
            ModelState.Remove(nameof(EmployeeVM.SubBranchName));

            ModelState.Remove(nameof(EmployeeVM.PhotoPath));


            // =====================================================
            // VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                // Restore HR-controlled display values
                model.EmployeeCode = employee.EmployeeCode;

                model.OfficialEmail = employee.OfficialEmail;

                model.DepartmentId = employee.DepartmentId;
                model.DesignationId = employee.DesignationId;
                model.ReportingManagerId = employee.ReportingManagerId;

                model.JoiningDate = employee.JoiningDate;

                model.DepartmentName =
                    await _context.Department
                        .Where(x => x.DepartmentId == employee.DepartmentId)
                        .Select(x => x.DepartmentName)
                        .FirstOrDefaultAsync();

                model.DesignationName =
                    await _context.Designation
                        .Where(x => x.DesignationId == employee.DesignationId)
                        .Select(x => x.DesignationName)
                        .FirstOrDefaultAsync();

                var manager = await _context.Employee
                    .Where(x => x.EmployeeId == employee.ReportingManagerId)
                    .Select(x => new
                    {
                        x.FirstName,
                        x.LastName
                    })
                    .FirstOrDefaultAsync();

                model.ReportingManagerName =
                    manager != null
                        ? $"{manager.FirstName} {manager.LastName}".Trim()
                        : "Not assigned";

                model.IsActive = employee.IsActive;

                model.PhotoPath = employee.PhotoPath;

                return View(model);
            }


            // =====================================================
            // UPDATE EMPLOYEE PROFILE
            // =====================================================

            employee.FirstName =
                model.FirstName?.Trim();

            employee.LastName =
                model.LastName?.Trim();

            employee.Gender =
                model.Gender?.Trim();

            employee.DOB =
                model.DOB;

            employee.BloodGroup =
                model.BloodGroup?.Trim();

            employee.MaritalStatus =
                model.MaritalStatus?.Trim();

            employee.PersonalEmail =
                model.PersonalEmail?.Trim();

            employee.MobileNo =
                model.MobileNo?.Trim();

            employee.AlternateMobile =
                model.AlternateMobile?.Trim();

            employee.CurrentAddress =
                model.CurrentAddress?.Trim();

            employee.PermanentAddress =
                model.PermanentAddress?.Trim();


            // =====================================================
            // PHOTO
            // =====================================================

            if (model.Photo != null && model.Photo.Length > 0)
            {
                // Handle your existing photo upload logic here.
                // Example:
                //
                // employee.PhotoPath = await SaveEmployeePhoto(model.Photo);
            }


            // =====================================================
            // SAVE
            // =====================================================

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Profile updated successfully.";


            return RedirectToAction(nameof(UpdateProfile));
        }

        [HttpGet]
        public async Task<IActionResult> MarkAttendance()
        {
            //try
            {
                var currentUser =
                    await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Unauthorized();


                var employee =
                    await _context.Employee
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationUserId == currentUser.Id);


                if (employee == null)
                    return NotFound("Employee profile not found.");


                var today =
                    DateTime.Today;


                var attendance =
                    await _context.EmployeeAttendance
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == employee.EmployeeId &&
                            x.AttendanceDate == today);


                var model =
                    new EmployeeAttendanceVM
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        EmployeeCode =
                            employee.EmployeeCode,

                        EmployeeName =
                            $"{employee.FirstName} {employee.LastName}".Trim(),

                        AttendanceDate =
                            today,

                        InTime =
                            attendance?.InTime,

                        OutTime =
                            attendance?.OutTime,

                        AttendanceStatus =
                            attendance?.AttendanceStatus
                            ?? "Not Marked",

                        AttendanceSource =
                            attendance?.AttendanceSource
                            ?? "-"
                    };


                return View(model);
            }
            //catch (Exception)
            //{
            //    TempData["Error"] =
            //        "Unable to load attendance.";

            //    return RedirectToAction("Index", "Home");
            //}
        }
        
        [HttpPost]
        public async Task<IActionResult> CheckIn(
        double latitude,
        double longitude,
        double accuracy)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Json(new
                {
                    success = false,
                    message = "User session expired."
                });
            }

            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Employee profile not found."
                });
            }

            // Get address from coordinates
            var location =
                await GetLocationAddress(latitude, longitude);

            // =====================================================
            // OFFICE LOCATION
            // =====================================================

            //const double officeLatitude = 28.4515;

            //const double officeLongitude = 77.0718;

            const double officeLatitude = 28.569950;
            const double officeLongitude = 77.323406;
            const double allowedRadius = 2000; // 2 km




            // =====================================================
            // CHECK DISTANCE
            // =====================================================

            //double distance =
            //    CalculateDistanceInMeters(
            //        officeLatitude,
            //        officeLongitude,
            //        latitude,
            //        longitude);


            //if (distance > allowedRadius)
            //{
            //    return Json(new
            //    {
            //        success = false,

            //        message =
            //            $"You are outside the allowed attendance location. " +
            //            $"Distance from office: {distance / 1000:F2} km."
            //    });
            //}


            // =====================================================
            // TODAY
            // =====================================================

            var today =
                DateTime.Today;


            var attendance =
                await _context.EmployeeAttendance
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employee.EmployeeId &&
                        x.AttendanceDate == today);


            // =====================================================
            // CREATE ATTENDANCE
            // =====================================================

            if (attendance == null)
            {
                attendance =
                    new EmployeeAttendance
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        EmployeeCode =
                            employee.EmployeeCode,

                        AttendanceDate =
                            today,

                        InTime =
                            DateTime.Now,

                        AttendanceStatus =
                            "Present",

                        AttendanceSource =
                            "Manual",

                        CreatedDate =
                            DateTime.Now,

                        CheckInLatitude =
                            latitude,

                        CheckInLongitude =
                            longitude,

                        CheckInAccuracy =
                            accuracy,
                        CheckInLocation=location
                    };

                _context.EmployeeAttendance.Add(attendance);
            }
            else
            {
                // Prevent duplicate check-in

                if (attendance.InTime != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "You have already checked in today."
                    });
                }


                attendance.InTime =
                    DateTime.Now;

                attendance.AttendanceSource =
                    "Manual";

                attendance.CheckInLatitude =
                    latitude;

                attendance.CheckInLongitude =
                    longitude;

                attendance.CheckInAccuracy =
                    accuracy;
            }


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,

                message =
                    "Attendance marked successfully.",

                time =
                    attendance.InTime?
                        .ToString("hh:mm tt")
            });
        }
        private static double CalculateDistanceInMeters(
    double lat1,
    double lon1,
    double lat2,
    double lon2)
        {
            const double earthRadius = 6371000;

            double dLat =
                DegreesToRadians(lat2 - lat1);

            double dLon =
                DegreesToRadians(lon2 - lon1);


            double a =
                Math.Sin(dLat / 2) *
                Math.Sin(dLat / 2)
                +
                Math.Cos(
                    DegreesToRadians(lat1))
                *
                Math.Cos(
                    DegreesToRadians(lat2))
                *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);


            double c =
                2 *
                Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a));


            return earthRadius * c;
        }


        private static double DegreesToRadians(
            double degrees)
        {
            return degrees *
                   Math.PI /
                   180;
        }


        [HttpPost]
        
        public async Task<IActionResult> CheckOut(
    double latitude,
    double longitude,
    double accuracy)
        {
            try
            {
                var currentUser =
                    await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Your session has expired. Please login again."
                    });
                }

                var employee =
                    await _context.Employee
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Employee profile not found."
                    });
                }

                // =====================================================
                // VALIDATE LOCATION
                // =====================================================

                if (latitude == 0 || longitude == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unable to detect your location. Please try again."
                    });
                }

                // =====================================================
                // GET LOCATION ADDRESS
                // =====================================================

                var location =
                    await GetLocationAddress(latitude, longitude);

                var today =
                    DateTime.Today;

                // =====================================================
                // GET TODAY'S ATTENDANCE
                // =====================================================

                var attendance =
                    await _context.EmployeeAttendance
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == employee.EmployeeId &&
                            x.AttendanceDate == today);

                // =====================================================
                // NO CHECK-IN
                // =====================================================

                if (attendance == null ||
                    attendance.InTime == null)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Please check in before checking out."
                    });
                }

                // =====================================================
                // ALREADY CHECKED OUT
                // =====================================================

                if (attendance.OutTime != null)
                {
                    return Json(new
                    {
                        success = false,

                        message =
                            "You have already checked out today.",

                        time =
                            attendance.OutTime
                                .Value
                                .ToString("hh:mm tt")
                    });
                }

                // =====================================================
                // CHECKOUT
                // =====================================================

                var now =
                    DateTime.Now;

                attendance.OutTime =
                    now;

                // =====================================================
                // SAVE CHECKOUT LOCATION
                // =====================================================

                attendance.CheckOutLocation =
                    location;
                attendance.CheckOutLatitude = latitude;
                attendance.CheckOutLongitude = longitude;
                attendance.CheckOutAccuracy = accuracy;
                // =====================================================
                // ATTENDANCE SOURCE / REMARKS
                // =====================================================

                if (attendance.AttendanceSource == "Biometric")
                {
                    attendance.Remarks =
                        string.IsNullOrWhiteSpace(attendance.Remarks)
                            ? "Check-out marked manually by employee."
                            : attendance.Remarks +
                              " Check-out marked manually by employee.";
                }
                else
                {
                    attendance.AttendanceSource =
                        "Manual";

                    attendance.Remarks =
                        "Check-out marked manually by employee.";
                }

                // =====================================================
                // SAVE
                // =====================================================

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,

                    message =
                        "Check-out marked successfully.",

                    time =
                        now.ToString("hh:mm tt"),

                    location =
                        location,

                    source =
                        attendance.AttendanceSource
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,

                    message =
                        "Unable to mark check-out. Please try again."
                });
            }
        }

        private async Task<string> GetLocationAddress(
    double latitude,
    double longitude)
        {
            try
            {
                using var client = new HttpClient();

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "TSSC-Unified-Portal/1.0");

                var url =
                    $"https://nominatim.openstreetmap.org/reverse" +
                    $"?lat={latitude}" +
                    $"&lon={longitude}" +
                    $"&format=json";

                var response =
                    await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "Location unavailable";

                var json =
                    await response.Content.ReadAsStringAsync();

                using var document =
                    JsonDocument.Parse(json);

                if (document.RootElement.TryGetProperty(
                    "display_name",
                    out var displayName))
                {
                    return displayName.GetString()
                           ?? "Location unavailable";
                }

                return "Location unavailable";
            }
            catch
            {
                return "Location unavailable";
            }
        }
    }
}
    

