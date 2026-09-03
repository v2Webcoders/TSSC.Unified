using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
using QUIZAPP.Services;

namespace TSSC.Unified.Areas.TP.Controllers
{
    [Area("TP")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppdbContext _context;
        private readonly EmailService _emailService;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            AppdbContext context,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
        }

        // =========================================================
        // TP REGISTRATION - GET
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // =========================================================
        // TP REGISTRATION - POST
        // =========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            TPRegistrationVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.OfficialEmail =
                model.OfficialEmail.Trim().ToLower();

            model.MobileNumber =
                model.MobileNumber.Trim();

            model.OrganizationName =
                model.OrganizationName.Trim();

            model.OrganizationType =
                model.OrganizationType.Trim();

            model.RegistrationNumber =
                model.RegistrationNumber.Trim();

            model.AuthorizedPersonName =
                model.AuthorizedPersonName.Trim();

            // =====================================================
            // CHECK EXISTING IDENTITY ACCOUNT
            // =====================================================

            var existingUser =
                await _userManager.FindByEmailAsync(
                    model.OfficialEmail);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "OfficialEmail",
                    "An account already exists with this Official Email ID.");

                return View(model);
            }

            // =====================================================
            // CHECK EXISTING AFFILIATION APPLICATION
            // =====================================================

            var existingApplication =
                await _context.AffiliationApplications
                    .AnyAsync(x =>
                        x.OfficialEmail == model.OfficialEmail);

            if (existingApplication)
            {
                ModelState.AddModelError(
                    "OfficialEmail",
                    "An affiliation application already exists with this Official Email ID.");

                return View(model);
            }

            // =====================================================
            // GENERATE TEMPORARY PASSWORD
            // =====================================================

            var temporaryPassword =
                GenerateTemporaryPassword();

            // =====================================================
            // CREATE IDENTITY USER
            // =====================================================

            var user = new AppUser
            {
                UserName = model.OfficialEmail,
                Email = model.OfficialEmail,

                Name = model.AuthorizedPersonName,
                MobileNo = model.MobileNumber,
                UserRole = "TP",

                EmailConfirmed = false,

                CreatedOn = DateTime.Now,
                UpdatedOn = DateTime.Now

                // DO NOT SET Password HERE
            };

            var userResult =
                await _userManager.CreateAsync(
                    user,
                    temporaryPassword);

            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            // =====================================================
            // ASSIGN TP ROLE
            // =====================================================

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "TP");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            // =====================================================
            // CREATE AFFILIATION APPLICATION
            // =====================================================

            var application =
                new AffiliationApplication
                {
                    ApplicantUserId = user.Id,

                    OrganizationName =
                        model.OrganizationName,

                    OrganizationType =
                        model.OrganizationType,

                    RegistrationNumber =
                        model.RegistrationNumber,

                    OfficialEmail =
                        model.OfficialEmail,

                    MobileNumber =
                        model.MobileNumber,

                    AuthorizedPersonName =
                        model.AuthorizedPersonName,

                    Status = "Draft",

                    CreatedBy = user.Id,

                    CreatedOn = DateTime.Now
                };

            _context.AffiliationApplications.Add(application);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Remove Identity account if application creation fails
                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to complete registration. Please try again.");

                return View(model);
            }

            // =====================================================
            // SEND LOGIN CREDENTIALS
            // =====================================================

            try
            {
                var replacements =
                    new Dictionary<string, string>
                    {
                        ["{{OrganizationName}}"] =
                            model.OrganizationName,

                        ["{{AuthorizedPersonName}}"] =
                            model.AuthorizedPersonName,

                        ["{{LoginId}}"] =
                            model.OfficialEmail,

                        ["{{Password}}"] =
                            temporaryPassword
                    };

                await _emailService.SendEmailAsync(
                    model.OfficialEmail,
                    "TP Registration - Login Credentials",
                    "TPRegistrationCredentials.html",
                    replacements);
            }
            catch
            {
                // Registration is already created.
                // Email failure should not delete the account.
            }

            TempData["RegistrationSuccess"] =
                "TP registration completed successfully. " +
                "Your login credentials have been sent to your Official Email ID.";

            return RedirectToAction(
                "Login",
                "Account",
                new
                {
                    area = "TP"
                });
        }

        // =========================================================
        // GENERATE TEMPORARY PASSWORD
        // =========================================================

        private string GenerateTemporaryPassword()
        {
            return $"Tssc@{Random.Shared.Next(100000, 999999)}";
        }

        // =========================================================
        // TP LOGIN - GET
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        // =========================================================
        // TP LOGIN - POST
        // =========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Official Email ID and Password are required.");

                return View();
            }

            email = email.Trim().ToLower();

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid Official Email ID or Password.");

                return View();
            }

            // =====================================================
            // VERIFY TP ROLE
            // =====================================================

            var isTP =
                await _userManager.IsInRoleAsync(
                    user,
                    "TP");

            if (!isTP)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This account is not registered as a TP.");

                return View();
            }

            // =====================================================
            // LOGIN
            // =====================================================

            var result =
                await _signInManager.PasswordSignInAsync(
                    user,
                    password,
                    isPersistent: false,
                    lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new
                    {
                        area = "TP"
                    });
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account is temporarily locked. Please try again later.");

                return View();
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid Official Email ID or Password.");

            return View();
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account",
                new
                {
                    area = "TP"
                });
        }
    }
}