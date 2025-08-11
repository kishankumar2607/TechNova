using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Models;

namespace TechNova.Controllers
{
    public class ProductsController : Controller
    {
        private AppDbContext context { get; set; }

        public ProductsController(AppDbContext ctx)
        {
            context = ctx;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var products = context.Products
                                  .AsNoTracking()
                                  .OrderByDescending(p => p.CreatedAt)
                                  .ToList();
            return View(products);
        }

        public IActionResult Details(int id)
        {
            var product = context.Products.FirstOrDefault(p => p.ProductID == id);
            if (product == null)
                return NotFound();

            // Example: Related products (simple logic)
            ViewBag.Related = context.Products
                .Where(p => p.ProductID != id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToList();

            return View(product);
        }
    }
}
