using Microsoft.AspNetCore.Mvc;
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
