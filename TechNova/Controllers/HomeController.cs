using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechNova.Models;

namespace TechNova.Controllers
{
    // main site controller (home pages)
    public class HomeController : Controller
    {
        // database context
        private readonly AppDbContext context;

        // get DbContext from DI
        public HomeController(AppDbContext ctx)
        {
            // store context for later use
            context = ctx;
        }

        // home page
        public IActionResult Index()
        {
            // read role from cookie
            var role = Request.Cookies["Role"];
            // if admin, send to admin dashboard
            if (role == "Admin")
            {
                // redirect admins to Admin/Product index
                return RedirectToAction("Index", "Product", new { area = "Admin" });
            }

            // fetch 4 oldest products for flash sales
            ViewBag.FlashSales = context.Products.OrderBy(p => p.CreatedAt).Take(4).ToList();
            // fetch 4 most-reviewed products for best sellers
            ViewBag.BestSellers = context.Products.OrderByDescending(p => p.ReviewCount).Take(4).ToList();
            // fetch next 8 products to explore
            ViewBag.Explore = context.Products.Skip(4).Take(8).ToList();
            // fetch 3 newest products for new arrivals
            ViewBag.NewArrivals = context.Products.OrderByDescending(p => p.CreatedAt).Take(3).ToList();

            // render home view
            return View();
        }

        // about page
        public IActionResult About()
        {
            // render about view
            return View();
        }

        // contact page
        public IActionResult Contact()
        {
            // render contact view
            return View();
        }

        // FAQ page
        public IActionResult FAQ()
        {
            // render FAQ view
            return View();
        }

        // privacy policy page
        public IActionResult Privacy()
        {
            // render privacy view
            return View();
        }

        // terms of use page
        public IActionResult TermsofUse()
        {
            // render terms view
            return View();
        }

        // error page (no caching)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // build error model with request id
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
