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
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace QUIZAPP.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> userManager;
        private readonly SignInManager<AppUser> signInManager;
        private readonly AppdbContext _context;
        private readonly UtilityService _us;
        private readonly EmailService _emailService;
        public AccountController(UtilityService us, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, AppdbContext context, EmailService emailService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            _context = context;
            _us = us;
            _emailService = emailService;
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Category()
        {
            return View();
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
                return RedirectToAction("Index", "Home", new { area = "HRMS" });

            if (roles.Contains("Letter"))
                return RedirectToAction("Index", "Home", new { area = "HRMS" });
            
            if (roles.Contains("TrainingPartner"))
                return RedirectToAction("Index", "Home", new { area = "TP" });

            if (roles.Contains("Finance"))
                return RedirectToAction("Index", "Home", new { area = "HRMS" });
            if (roles.Contains("CEO"))
                return RedirectToAction("Index", "Home", new { area = "HRMS" });
            if (roles.Contains("TOTTOAADMIN"))
                return RedirectToAction("Index", "Home", new { area = "HRMS" });
            if (roles.Contains("Trainer"))
                return RedirectToAction("Index", "Home", new { area = "Trainer" });
            if (roles.Contains("Agency"))
                return RedirectToAction("Index", "Home", new { area = "HRMS" });
            if (roles.Contains("StandardsTeam"))
                return RedirectToAction("Index", "Home", new { area = "HRMS" });

            return RedirectToAction("Index", "Home", new { area = "HRMS" });
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);

            // Don't reveal whether the email exists
            if (user == null)
            {
                TempData["Error"] =
                    "No account was found with this email address.";

                return RedirectToAction(nameof(ForgotPassword));
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new
                {
                    userId = user.Id,
                    token = token
                },
                protocol: Request.Scheme);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "EmailTemplates",
                "ForgotPassword.html");

            var replacements = new Dictionary<string, string>
    {
        { "BaseUrl", baseUrl },
        { "Name", user.Name ?? "Employee" },
        { "ResetLink", resetLink ?? "" }
    };

            var emailResult = await _emailService.SendEmailAsync(
                model.Email,
                "TSSC - Reset Your Password",
                templatePath,
                replacements);

            if (emailResult != "True")
            {
                TempData["Error"] =
                    "Unable to send the password reset email. Please try again.";

                return RedirectToAction(nameof(ForgotPassword));
            }

            TempData["ForgotPasswordMessage"] =
                "Password reset link has been sent to your email address. Please check your inbox.";

            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("Invalid password reset link.");
            }

            var model = new ResetPasswordVM
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid password reset request.");

                return View(model);
            }

            var result = await userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.Password);

            if (result.Succeeded)
            {
                // Keep custom AppUser.Password field in sync
                user.Password = model.Password;

                await userManager.UpdateAsync(user);

                TempData["PasswordResetSuccess"] =
       "Your password has been reset successfully. You can now sign in.";


                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
    }

}
