using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Models;

namespace TechNova.Controllers
{
    // handles product listing and details pages
    public class ProductsController : Controller
    {
        // database context
        private AppDbContext context { get; set; }

        // inject DbContext
        public ProductsController(AppDbContext ctx)
        {
            context = ctx;
        }

        // GET: show all products (newest first)
        [HttpGet]
        public IActionResult Index()
        {
            // read products without tracking, order by CreatedAt desc
            var products = context.Products
                                  .AsNoTracking()
                                  .OrderByDescending(p => p.CreatedAt)
                                  .ToList();
            // render list view
            return View(products);
        }

        // GET: show one product by id
        public IActionResult Details(int id)
        {
            // find product by id
            var product = context.Products.FirstOrDefault(p => p.ProductID == id);
            // if not found, return 404
            if (product == null)
                return NotFound();

            // load related products (not the same id), newest first, top 4
            ViewBag.Related = context.Products
                .Where(p => p.ProductID != id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToList();

            // render details view
            return View(product);
        }
    }
}
