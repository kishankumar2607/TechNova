using Microsoft.AspNetCore.Mvc;

namespace TechNova.Areas.Admin.Controllers
{
    public class AdminBaseController : Controller
    {
        protected bool IsAdmin()
        {
            return HttpContext.Request.Cookies["Role"] == "Admin";
        }

        protected IActionResult RedirectIfNotAdmin()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            return null;
        }
    }
}
