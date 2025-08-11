using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TechNova.Controllers
{
    public class ErrorController : Controller
    {
        // Pretty 404 page
        [Route("Error/404")]
        public IActionResult Error404() => View("Error404");

        // Unhandled exceptions (500, etc.) via UseExceptionHandler("/Error/500")
        [Route("Error")]
        [Route("Error/500")]
        public IActionResult Error500()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            ViewBag.Path = feature?.Path;
            ViewBag.Message = feature?.Error?.Message;
            return View("ErrorGeneric"); // -> Views/Error/ErrorGeneric.cshtml
        }

        // Status codes via UseStatusCodePagesWithReExecute("/Error/{0}")
        [Route("Error/{code:int}")]
        public IActionResult GeneralError(int code)
        {
            if (code == 404) return RedirectToAction(nameof(Error404));
            ViewBag.StatusCode = code;
            return View("ErrorGeneric"); // -> Views/Error/ErrorGeneric.cshtml
        }
    }
}