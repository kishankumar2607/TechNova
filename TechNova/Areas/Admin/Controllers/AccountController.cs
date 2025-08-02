using Microsoft.AspNetCore.Mvc;

namespace TechNova.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        public IActionResult Logout()
        {
            Response.Cookies.Delete("UserName");
            Response.Cookies.Delete("UserEmail");
            Response.Cookies.Delete("UserId");
            Response.Cookies.Delete("Role");

            return RedirectToAction("Index", "Home");
        }
    }
}
