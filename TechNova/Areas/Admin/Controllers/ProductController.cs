using Microsoft.AspNetCore.Mvc;
using TechNova.Models;

namespace TechNova.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : AdminBaseController
    {

        private AppDbContext context { get; set; }

        public ProductController(AppDbContext ctx)
        {
            context = ctx;
        }

        public IActionResult Index()
        {
            var redirect = RedirectIfNotAdmin();
            if (redirect != null) return redirect;

            return View(context.Products.ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            var redirect = RedirectIfNotAdmin();
            if (redirect != null) return redirect;

            return View();
        }

        [HttpPost]
        public IActionResult Create(Product product)
        {
            var redirect = RedirectIfNotAdmin();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).ToList();
                return View(product);
            }

            // Calculate discounted price
            if (product.DiscountPercent.HasValue && product.DiscountPercent > 0)
            {
                product.DiscountedPrice = product.Price - (product.Price * (product.DiscountPercent.Value / 100));
            }
            else
            {
                product.DiscountedPrice = product.Price;
            }

            product.CreatedAt = DateTime.Now;
            product.UpdatedAt = DateTime.Now;

            context.Products.Add(product);
            context.SaveChanges();

            return RedirectToAction("Index");
        }


        public IActionResult Edit(int id)
        {
            var redirect = RedirectIfNotAdmin();
            if (redirect != null) return redirect;

            var product = context.Products.Find(id);
            return View(product);
        }

        [HttpPost]
        public IActionResult Edit(Product product)
        {
            var redirect = RedirectIfNotAdmin();
            if (redirect != null) return redirect;

            if (ModelState.IsValid)
            {
                product.UpdatedAt = DateTime.Now;
                context.Products.Update(product);
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(product);
        }

        public IActionResult Delete(int id)
        {
            var redirect = RedirectIfNotAdmin();
            if (redirect != null) return redirect;

            var product = context.Products.Find(id);
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var redirect = RedirectIfNotAdmin();
            if (redirect != null) return redirect;

            var product = context.Products.Find(id);
            if (product != null)
            {
                context.Products.Remove(product);
                context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}