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

            // hash the entered password
            string hashedPassword = PasswordHelper.HashPassword(model.Password);

            // try to find a user with this email + hashed password
            var existingUser = context.Users
                .FirstOrDefault(u => u.Email == model.Email && u.Password == hashedPassword);

            if (existingUser != null)
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
    }
}
