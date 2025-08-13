using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Helper;
using TechNova.Models;

namespace TechNova.Controllers
{
    public class WishlistController : Controller
    {
        private readonly AppDbContext context;
        private const string WISHLIST_KEY = "WISHLIST";
        private const string CART_KEY = "CART";

        public WishlistController(AppDbContext ctx) => context = ctx;

        // --- session helpers ---
        private List<WishlistItem> GetWishlist()
            => HttpContext.Session.GetObject<List<WishlistItem>>(WISHLIST_KEY) ?? new List<WishlistItem>();
        private void SaveWishlist(List<WishlistItem> list)
            => HttpContext.Session.SetObject(WISHLIST_KEY, list);

        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
        private void SaveCart(List<CartItem> cart)
            => HttpContext.Session.SetObject(CART_KEY, cart);

        // GET /Wishlist
        [HttpGet]
        public IActionResult Index()
        {
            var items = GetWishlist();
            // Optional: refresh pricing from DB so it’s always current
            if (items.Count > 0)
            {
                var ids = items.Select(i => i.ProductID).ToList();
                var fresh = context.Products.AsNoTracking()
                             .Where(p => ids.Contains(p.ProductID))
                             .ToDictionary(p => p.ProductID);

                foreach (var w in items)
                {
                    if (fresh.TryGetValue(w.ProductID, out var p))
                    {
                        w.Name = p.Name;
                        w.ImageURL = p.ImageURL;
                        w.Price = p.Price;
                        w.DiscountedPrice = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                                              ? p.DiscountedPrice : null;
                        w.DiscountPercent = p.DiscountPercent;
                    }
                }
                SaveWishlist(items);
            }

            // simple “Just for you” section
            ViewBag.JustForYou = context.Products.AsNoTracking()
                                  .OrderByDescending(p => p.CreatedAt)
                                  .Take(4).ToList();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, string? returnUrl = null)
        {
            var p = context.Products.AsNoTracking().FirstOrDefault(x => x.ProductID == productId);
            if (p == null) return NotFound();

            var list = GetWishlist();
            if (!list.Any(x => x.ProductID == productId))
            {
                list.Add(new WishlistItem
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    ImageURL = p.ImageURL,
                    Price = p.Price,
                    DiscountedPrice = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                                      ? p.DiscountedPrice : null,
                    DiscountPercent = p.DiscountPercent
                });
                SaveWishlist(list);
            }

            TempData["Success"] = $"{p.Name} added to your wishlist.";

            const string anchor = "#wish-success";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                // strip any existing hash and append our anchor
                var hash = returnUrl.IndexOf('#');
                var clean = hash >= 0 ? returnUrl.Substring(0, hash) : returnUrl;
                return Redirect(clean + anchor);
            }

            return Redirect(Url.Action(nameof(Index)) + anchor);
        }


        // POST /Wishlist/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId, string? returnUrl = null)
        {
            var list = GetWishlist();
            list.RemoveAll(x => x.ProductID == productId);
            SaveWishlist(list);

            TempData["Success"] = "Removed from wishlist.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // POST /Wishlist/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            SaveWishlist(new List<WishlistItem>());
            TempData["Success"] = "Wishlist cleared.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Wishlist/MoveToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoveToCart(int productId, int qty = 1)
        {
            qty = Math.Max(1, Math.Min(10, qty));

            var list = GetWishlist();
            var w = list.FirstOrDefault(x => x.ProductID == productId);
            if (w == null) return RedirectToAction(nameof(Index));

            var p = context.Products.AsNoTracking().FirstOrDefault(x => x.ProductID == productId);
            if (p == null) return RedirectToAction(nameof(Index));

            var unit = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                        ? p.DiscountedPrice.Value
                        : p.Price;

            var cart = GetCart();
            var existing = cart.FirstOrDefault(x => x.ProductID == productId);
            if (existing == null)
                cart.Add(new CartItem { ProductID = p.ProductID, Name = p.Name, ImageURL = p.ImageURL, UnitPrice = unit, Qty = qty });
            else
                existing.Qty = Math.Max(1, Math.Min(10, existing.Qty + qty));

            SaveCart(cart);

            // Option: remove from wishlist when moved
            list.RemoveAll(x => x.ProductID == productId);
            SaveWishlist(list);

            TempData["CartMsg"] = $"{p.Name} moved to cart.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Wishlist/MoveAllToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoveAllToCart()
        {
            var list = GetWishlist();
            if (list.Count == 0) return RedirectToAction(nameof(Index));

            var ids = list.Select(i => i.ProductID).ToList();
            var products = context.Products.AsNoTracking()
                             .Where(p => ids.Contains(p.ProductID))
                             .ToDictionary(p => p.ProductID);

            var cart = GetCart();

            foreach (var w in list)
            {
                if (!products.TryGetValue(w.ProductID, out var p)) continue;

                var unit = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                            ? p.DiscountedPrice.Value
                            : p.Price;

                var existing = cart.FirstOrDefault(x => x.ProductID == p.ProductID);
                if (existing == null)
                    cart.Add(new CartItem { ProductID = p.ProductID, Name = p.Name, ImageURL = p.ImageURL, UnitPrice = unit, Qty = 1 });
                else
                    existing.Qty = Math.Max(1, Math.Min(10, existing.Qty + 1));
            }

            SaveCart(cart);
            SaveWishlist(new List<WishlistItem>()); // clear after move
            TempData["CartMsg"] = "All wishlist items moved to cart.";
            return RedirectToAction(nameof(Index));
        }
    }
}