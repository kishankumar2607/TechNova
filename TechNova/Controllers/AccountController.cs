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

        // GET: /Account/Login
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
                HttpContext.Session.SetString("UserName", existingUser.FullName);
                HttpContext.Session.SetInt32("UserId", existingUser.UserId);
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
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
