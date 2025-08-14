using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Helpers;
using TechNova.Models;
using Microsoft.AspNetCore.Http;

namespace TechNova.Controllers
{
    public class AccountController : Controller
    {
        // holds the database connection/context
        private AppDbContext context { get; set; }

        // get the database context from DI
        public AccountController(AppDbContext ctx)
        {
            context = ctx;
        }

        // show login page (can carry a return URL)
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // handle login form submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            // if form has errors, show the same page
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // find by email, then verify provided password against stored hash
            var existingUser = context.Users.FirstOrDefault(u => u.Email == model.Email);

            if (existingUser != null &&
                PasswordHelper.VerifyPassword(model.Password, existingUser.Password))
            {
                // cookie settings (expiry, security, samesite)
                var options = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(1),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Strict
                };

                // save user info in cookies
                Response.Cookies.Append("UserName", existingUser.FullName, options);
                Response.Cookies.Append("UserEmail", existingUser.Email, options);
                Response.Cookies.Append("UserId", existingUser.UserId.ToString(), options);
                Response.Cookies.Append("Role", existingUser.Role, options);

                // go back to local returnUrl if provided
                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                // send admins to admin area, others to home
                if (existingUser.Role == "Admin")
                    return RedirectToAction("Index", "Product", new { area = "Admin" });

                return RedirectToAction("Index", "Home");
            }

            // login failed
            ViewBag.Error = "Invalid email or password.";
            return View(model);
        }

        // show registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // handle registration form submit
        [HttpPost]
        public IActionResult Register(User user)
        {
            // if form is valid, continue
            if (ModelState.IsValid)
            {
                // stop duplicate accounts by email
                var exists = context.Users.Any(u => u.Email == user.Email);
                if (exists)
                {
                    ViewBag.Error = "Email already registered.";
                    return View(user);
                }

                // hash password and save user
                user.Password = PasswordHelper.HashPassword(user.Password);
                context.Users.Add(user);
                context.SaveChanges();

                // after signup, go to login
                return RedirectToAction("Login");
            }

            // show validation errors
            return View(user);
        }

        // log out by clearing cookies
        public IActionResult Logout()
        {
            Response.Cookies.Delete("UserName");
            Response.Cookies.Delete("UserEmail");
            Response.Cookies.Delete("UserId");
            Response.Cookies.Delete("Role");
            return RedirectToAction("Index", "Home");
        }

        // simple guard for account page
        public IActionResult MyAccount()
        {
            // if no user cookie, force login
            if (string.IsNullOrEmpty(Request.Cookies["UserName"]))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // GET Account Manage
        [HttpGet]
        public IActionResult Manage()
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(Manage), "Account");
                return RedirectToAction("Login", new { returnUrl });
            }
            if (!int.TryParse(Request.Cookies["UserId"], out var userId)) return RedirectToAction("Login");

            var user = context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == userId);
            if (user == null) return NotFound();

            var vm = new AccountSettingsViewModel
            {
                FullName = user.FullName,
                Email = user.Email
            };
            ViewBag.ChangePassword = new ChangePasswordViewModel();
            return View(vm);
        }

        // POST Account UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(AccountSettingsViewModel vm)
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(Manage), "Account");
                return RedirectToAction("Login", new { returnUrl });
            }
            if (!int.TryParse(Request.Cookies["UserId"], out var userId)) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                ViewBag.ChangePassword = new ChangePasswordViewModel();
                return View("Manage", vm);
            }

            // ensure email unique (except self)
            var emailUsed = context.Users.Any(u => u.Email == vm.Email && u.UserId != userId);
            if (emailUsed)
            {
                ModelState.AddModelError(nameof(vm.Email), "That email is already in use.");
                ViewBag.ChangePassword = new ChangePasswordViewModel();
                return View("Manage", vm);
            }

            var user = context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return NotFound();

            user.FullName = vm.FullName;
            user.Email = vm.Email;
            context.SaveChanges();

            TempData["FlashSuccess"] = "Profile updated.";
            return RedirectToAction(nameof(Manage));
        }

        // POST Account ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel vm)
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(Manage), "Account");
                return RedirectToAction("Login", new { returnUrl });
            }
            if (!int.TryParse(Request.Cookies["UserId"], out var userId)) return RedirectToAction("Login");

            // Rehydrate profile fields for the same page render
            var user = context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return NotFound();

            var profileVm = new AccountSettingsViewModel
            {
                FullName = user.FullName,
                Email = user.Email
            };

            if (!ModelState.IsValid)
            {
                ViewBag.ChangePassword = vm;
                return View("Manage", profileVm);
            }

            // verify current password (stored as SHA-256 hex)
            if (!PasswordHelper.VerifyPassword(vm.CurrentPassword, user.Password))
            {
                ModelState.AddModelError(nameof(vm.CurrentPassword), "Current password is incorrect.");
                ViewBag.ChangePassword = vm;
                return View("Manage", profileVm);
            }

            // store new password as hash
            user.Password = PasswordHelper.HashPassword(vm.NewPassword);
            context.SaveChanges();

            TempData["FlashSuccess"] = "Password changed.";
            return RedirectToAction(nameof(Manage));
        }
    }
}
