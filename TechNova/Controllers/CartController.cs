using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Helper;
using TechNova.Models;

namespace TechNova.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext context;
        private const string CART_KEY = "CART";
        private const decimal TAX_RATE = 0.13m;

        public CartController(AppDbContext ctx) => context = ctx;

        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        private void SaveCart(List<CartItem> cart)
            => HttpContext.Session.SetObject(CART_KEY, cart);

        // GET /Cart
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();
            var subtotal = cart.Sum(i => i.LineTotal);
            var shipping = subtotal > 500m ? 0m : (subtotal == 0 ? 0m : 30m);
            var tax = Math.Round(subtotal * TAX_RATE, 2, MidpointRounding.AwayFromZero);

            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.TaxRate = TAX_RATE;
            ViewBag.Tax = tax;
            ViewBag.Total = subtotal + shipping + tax;

            return View(cart);
        }

        // POST /Cart/Add  (AJAX or normal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, int qty = 1)
        {
            qty = Math.Max(1, Math.Min(10, qty));

            var p = context.Products.AsNoTracking().FirstOrDefault(x => x.ProductID == productId);
            if (p == null) return NotFound();

            var unit = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                        ? p.DiscountedPrice.Value
                        : p.Price;

            var cart = GetCart();
            var existing = cart.FirstOrDefault(x => x.ProductID == productId);

            if (existing == null)
            {
                cart.Add(new CartItem
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    ImageURL = p.ImageURL,
                    UnitPrice = unit,
                    Qty = qty
                });
            }
            else
            {
                existing.Qty = Math.Max(1, Math.Min(10, existing.Qty + qty));
                existing.UnitPrice = unit;
            }

            SaveCart(cart);

            var message = $"{p.Name} added to cart.";
            var count = cart.Sum(i => i.Qty);
            var subtotal = cart.Sum(i => i.LineTotal);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    ok = true,
                    message,
                    count,
                    subtotal = subtotal.ToString("C")
                });
            }

            TempData["CartMsg"] = message;
            return RedirectToAction(nameof(Index));
        }

        // POST /Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int qty)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductID == productId);
            if (item != null)
            {
                item.Qty = Math.Max(1, Math.Min(10, qty));
                SaveCart(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(x => x.ProductID == productId);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        // POST /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            SaveCart(new List<CartItem>());
            return RedirectToAction(nameof(Index));
        }
    }
}
