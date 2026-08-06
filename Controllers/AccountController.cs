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
        private string Name = "Seastar";
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
            ViewBag.Title = Name + " | Login";
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewBag.Title = Name + " | Login";

            // Validation
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter User Name and Password.";
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.userid);

            if (user == null)
            {
                TempData["ErrorMessage"] = "You have entered an invalid username or password.";
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(
                model.userid,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "You have entered an invalid username or password.";
                return View(model);
            }

            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault()?.ToLower();

            if (role == "admin")
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            if (role == "hr" || role =="employee")
            {
                return RedirectToAction("Index", "Home", new { area = "HRMS" });
            }

            return RedirectToAction("Index", "Home");
        }
        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<IActionResult> login(LoginViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        ViewBag.Title = Name + " | Login";
        //        var user = await userManager.FindByEmailAsync(model.userid);
        //        var result = await signInManager.PasswordSignInAsync(model.userid, model.Password, false, false);
        //        string redirectUrl = "/Account/Login";
        //        if (result.Succeeded)
        //        {
        //            var roles = await userManager.GetRolesAsync(user);
        //            var singleRole = roles.FirstOrDefault(); // Get the first role, if exists
        //            if (singleRole.ToLower() == "admin")
        //            {
        //                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        //                string browserInfo = Request.Headers["User-Agent"].ToString();

        //                // CALL LOG SERVICE HERE
        //                await _us.LogUserActionAsync(
        //                   user.UserName,
        //                    "Admin",
        //                    "Login",
        //                    ipAddress,
        //                    browserInfo
        //                );

        //                redirectUrl = "/Admin/Home/Index";
        //            }
        //            else if (singleRole.ToLower() == "hr")
        //            {
        //                redirectUrl = "/HRMS/Home/Index";
        //            }
        //            else
        //            {
        //                redirectUrl = "/Home/Index";
        //            }
        //            //return RedirectToAction("Admin","index", "home");
        //            return RedirectPermanent(redirectUrl);
        //        }
        //        else
        //        {
        //            TempData["ErrorMessage"] = "You have entered an invalid username or password";
        //            ModelState.AddModelError(string.Empty, "You have entered an invalid username or password");

        //            return View(model);
        //        }
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "You have entered an invalid username or password";
        //        ModelState.AddModelError(string.Empty, "You have entered an invalid username or password");
        //        return View(model);
        //    }

        //}

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
