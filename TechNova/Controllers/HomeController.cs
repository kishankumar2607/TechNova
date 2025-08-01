using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechNova.Models;

namespace TechNova.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext context;

        public HomeController(AppDbContext ctx)
        {
            context = ctx;
        }

        public IActionResult Index()
        {
            ViewBag.FlashSales = context.Products.OrderBy(p => p.CreatedAt).Take(4).ToList();
            ViewBag.BestSellers = context.Products.OrderByDescending(p => p.ReviewCount).Take(4).ToList();
            ViewBag.Explore = context.Products.Skip(4).Take(8).ToList();
            ViewBag.NewArrivals = context.Products.OrderByDescending(p => p.CreatedAt).Take(3).ToList();

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult TermsofUse()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
