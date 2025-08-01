using Microsoft.AspNetCore.Mvc;

namespace TechNova.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/404")]
        public IActionResult Error404()
        {
            return View("Error404");
        }

        // Optional: catch-all fallback for other errors
        [Route("Error/{code}")]
        public IActionResult GeneralError(int code)
        {
            if (code == 404) return RedirectToAction("Error404");
            return View("ErrorGeneric");
        }
    }
}
