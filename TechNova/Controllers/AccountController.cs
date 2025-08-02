using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Helpers;
using TechNova.Models;

namespace TechNova.Controllers
{
    public class AccountController : Controller
    {
        private AppDbContext context { get; set; }

        public AccountController(AppDbContext ctx)
        {
            context = ctx;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string hashedPassword = PasswordHelper.HashPassword(model.Password);

            var existingUser = context.Users
                .FirstOrDefault(u => u.Email == model.Email && u.Password == hashedPassword);

            if (existingUser != null)
            {
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(1),
                    HttpOnly = true,
                    Secure = true, // set to false if not using HTTPS
                    SameSite = SameSiteMode.Strict
                };

                Response.Cookies.Append("UserName", existingUser.FullName, options);
                Response.Cookies.Append("UserEmail", existingUser.Email, options);
                Response.Cookies.Append("UserId", existingUser.UserId.ToString(), options);
                Response.Cookies.Append("Role", existingUser.Role, options);

                if (existingUser.Role == "Admin")
                    return RedirectToAction("Index", "Product", new { area = "Admin" });

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid email or password.";
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() 
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                var exists = context.Users.Any(u => u.Email == user.Email);
                if (exists)
                {
                    ViewBag.Error = "Email already registered.";
                    return View(user);
                }

                user.Password = PasswordHelper.HashPassword(user.Password);
                context.Users.Add(user);
                context.SaveChanges();

                return RedirectToAction("Login");
            }

            return View(user);
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("UserName");
            Response.Cookies.Delete("UserEmail");
            Response.Cookies.Delete("UserId");
            Response.Cookies.Delete("Role");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult MyAccount()
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserName"]))
            {
                return RedirectToAction("Login");
            }
            return View();
        }
    }
}
