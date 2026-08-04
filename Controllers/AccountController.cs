using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Mvc.Rendering;
using QUIZAPP.ViewModel;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;
using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;

namespace QUIZAPP.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> userManager;
        private readonly SignInManager<AppUser> signInManager;
        private readonly AppdbContext _context;
        private readonly UtilityService _us;
        public AccountController(UtilityService us, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, AppdbContext context)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            _context = context;
            _us = us;
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult login()
        {
            var items = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Option 1" },
        new SelectListItem { Value = "2", Text = "Option 2" },
        new SelectListItem { Value = "3", Text = "Option 3" }
    };
            ViewBag.Items = items;
            ViewBag.Title = "Login";
            return View();
        }
        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    ViewBag.Title = Name + " | Login";

        //    // Validation
        //    if (!ModelState.IsValid)
        //    {
        //        TempData["ErrorMessage"] = "Please enter User Name and Password.";
        //        return View(model);
        //    }

        //    var user = await userManager.FindByEmailAsync(model.userid);

        //    if (user == null)
        //    {
        //        TempData["ErrorMessage"] = "You have entered an invalid username or password.";
        //        return View(model);
        //    }

        //    var result = await signInManager.PasswordSignInAsync(
        //        model.userid,
        //        model.Password,
        //        isPersistent: false,
        //        lockoutOnFailure: false);

        //    if (!result.Succeeded)
        //    {
        //        TempData["ErrorMessage"] = "You have entered an invalid username or password.";
        //        return View(model);
        //    }

        //    var roles = await userManager.GetRolesAsync(user);
        //    var role = roles.FirstOrDefault()?.ToLower();

        //    if (role == "admin")
        //    {
        //        return RedirectToAction("Index", "Home", new { area = "Admin" });
        //    }

        //    if (role == "hr")
        //    {
        //        return RedirectToAction("Index", "Home", new { area = "HRMS" });
        //    }

        //    return RedirectToAction("Index", "Home");
        //}
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewBag.Title = "Login";

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter User Name and Password.";
                return View(model);
            }

            // Clear any existing login
            await signInManager.SignOutAsync();

            // Find user by username (not email)
            var user = await userManager.FindByNameAsync(model.userid);

            if (user == null)
            {
                TempData["ErrorMessage"] = "You have entered an invalid username or password.";
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "You have entered an invalid username or password.";
                return View(model);
            }

            var roles = await userManager.GetRolesAsync(user);
            
            if (roles.Contains("Admin"))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            if (roles.Contains("HR"))
                return RedirectToAction("Index", "Home", new { area = "HRMS" });

            if (roles.Contains("Employee"))
                return RedirectToAction("Index", "Home", new { area = "Employee" });

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            string userName = User.Identity.Name;
            await signInManager.SignOutAsync();
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string browserInfo = Request.Headers["User-Agent"].ToString();

            // CALL LOG SERVICE HERE
            await _us.LogUserActionAsync(
              userName,
                "Admin",
                "Logout",
                ipAddress,
                browserInfo
            );

            return Redirect(Url.Action("login", "Account"));
        }
    }
}
