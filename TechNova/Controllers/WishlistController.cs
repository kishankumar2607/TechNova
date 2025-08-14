using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Helpers;
using TechNova.Models;

namespace TechNova.Controllers
{
    // Controller for wishlist features
    public class WishlistController : Controller
    {
        // Database context
        private readonly AppDbContext context;
        // Session key for wishlist
        private const string WISHLIST_KEY = "WISHLIST";
        // Session key for cart
        private const string CART_KEY = "CART";

        // Get DbContext from DI
        public WishlistController(AppDbContext ctx) => context = ctx;

        // Read wishlist from session (or return empty list)
        private List<WishlistItem> GetWishlist()
            => HttpContext.Session.GetObject<List<WishlistItem>>(WISHLIST_KEY) ?? new List<WishlistItem>();

        // Save wishlist to session
        private void SaveWishlist(List<WishlistItem> list)
            => HttpContext.Session.SetObject(WISHLIST_KEY, list);

        // Read cart from session (or return empty list)
        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        // Save cart to session
        private void SaveCart(List<CartItem> cart)
            => HttpContext.Session.SetObject(CART_KEY, cart);

        // Show wishlist page and refresh each item from DB
        [HttpGet]
        public IActionResult Index()
        {
            // get wishlist from session
            var items = GetWishlist();

            // if we have items, refresh their details from database
            if (items.Count > 0)
            {
                // collect all product IDs
                var ids = items.Select(i => i.ProductID).ToList();

                // fetch fresh product data and index by ProductID
                var fresh = context.Products.AsNoTracking()
                             .Where(p => ids.Contains(p.ProductID))
                             .ToDictionary(p => p.ProductID);

                // update wishlist items with latest info
                foreach (var w in items)
                {
                    if (fresh.TryGetValue(w.ProductID, out var p))
                    {
                        // sync fields from DB
                        w.Name = p.Name;
                        w.ImageURL = p.ImageURL;
                        w.Price = p.Price;
                        w.DiscountedPrice = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                                              ? p.DiscountedPrice : null;
                        w.DiscountPercent = p.DiscountPercent;
                    }
                }

                // save updated wishlist back to session
                SaveWishlist(items);
            }

            // load suggested products
            ViewBag.JustForYou = context.Products.AsNoTracking()
                                  .OrderByDescending(p => p.CreatedAt)
                                  .Take(4).ToList();

            // render view with wishlist items
            return View(items);
        }

        // Add a product to wishlist (avoids duplicates)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, string? returnUrl = null)
        {
            // get product by id
            var p = context.Products.AsNoTracking().FirstOrDefault(x => x.ProductID == productId);
            // if not found, 404
            if (p == null) return NotFound();

            // get current wishlist
            var list = GetWishlist();

            // only add if not already present
            if (!list.Any(x => x.ProductID == productId))
            {
                // add new wishlist item
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

                // save wishlist
                SaveWishlist(list);
            }

            // success message
            TempData["Success"] = $"{p.Name} added to your wishlist.";

            // anchor to focus the message area
            const string anchor = "#wish-success";

            // if a safe returnUrl was provided, redirect back with anchor
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                // strip existing hash so we can add our anchor
                var hash = returnUrl.IndexOf('#');
                var clean = hash >= 0 ? returnUrl.Substring(0, hash) : returnUrl;
                return Redirect(clean + anchor);
            }

            // fallback to wishlist index with anchor
            return Redirect(Url.Action(nameof(Index)) + anchor);
        }

        // Remove a single product from wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId, string? returnUrl = null)
        {
            // get wishlist
            var list = GetWishlist();
            // remove product by id
            list.RemoveAll(x => x.ProductID == productId);
            // save wishlist
            SaveWishlist(list);

            // show success message
            TempData["Success"] = "Removed from wishlist.";

            // return to given page if safe
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // otherwise go to wishlist
            return RedirectToAction(nameof(Index));
        }

        // Clear the whole wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            // save empty wishlist
            SaveWishlist(new List<WishlistItem>());
            // success message
            TempData["Success"] = "Wishlist cleared.";
            // back to wishlist page
            return RedirectToAction(nameof(Index));
        }

        // Move one item from wishlist to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoveToCart(int productId, int qty = 1)
        {
            // keep qty between 1 and 10
            qty = Math.Max(1, Math.Min(10, qty));

            // get wishlist and target item
            var list = GetWishlist();
            var w = list.FirstOrDefault(x => x.ProductID == productId);
            // if not in wishlist, just return
            if (w == null) return RedirectToAction(nameof(Index));

            // fetch product again for current pricing
            var p = context.Products.AsNoTracking().FirstOrDefault(x => x.ProductID == productId);
            // if product not found, just return
            if (p == null) return RedirectToAction(nameof(Index));

            // choose unit price (discounted if valid)
            var unit = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                        ? p.DiscountedPrice.Value
                        : p.Price;

            // read cart and look for existing line
            var cart = GetCart();
            var existing = cart.FirstOrDefault(x => x.ProductID == productId);

            // add new cart line or update qty
            if (existing == null)
                cart.Add(new CartItem { ProductID = p.ProductID, Name = p.Name, ImageURL = p.ImageURL, UnitPrice = unit, Qty = qty });
            else
                existing.Qty = Math.Max(1, Math.Min(10, existing.Qty + qty));

            // save cart
            SaveCart(cart);

            // remove item from wishlist and save
            list.RemoveAll(x => x.ProductID == productId);
            SaveWishlist(list);

            // message for cart action
            TempData["CartMsg"] = $"{p.Name} moved to cart.";
            // back to wishlist page
            return RedirectToAction(nameof(Index));
        }

        // Move all wishlist items to cart (adds one each)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoveAllToCart()
        {
            // get wishlist
            var list = GetWishlist();
            // nothing to move
            if (list.Count == 0) return RedirectToAction(nameof(Index));

            // fetch all products in one go
            var ids = list.Select(i => i.ProductID).ToList();
            var products = context.Products.AsNoTracking()
                             .Where(p => ids.Contains(p.ProductID))
                             .ToDictionary(p => p.ProductID);

            // read cart
            var cart = GetCart();

            // loop through wishlist items
            foreach (var w in list)
            {
                // skip if product missing
                if (!products.TryGetValue(w.ProductID, out var p)) continue;

                // pick price (discounted if valid)
                var unit = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                            ? p.DiscountedPrice.Value
                            : p.Price;

                // if not in cart, add with qty 1; else bump qty by 1 (max 10)
                var existing = cart.FirstOrDefault(x => x.ProductID == p.ProductID);
                if (existing == null)
                    cart.Add(new CartItem { ProductID = p.ProductID, Name = p.Name, ImageURL = p.ImageURL, UnitPrice = unit, Qty = 1 });
                else
                    existing.Qty = Math.Max(1, Math.Min(10, existing.Qty + 1));
            }

            // save cart and clear wishlist
            SaveCart(cart);
            SaveWishlist(new List<WishlistItem>());

            // confirmation message
            TempData["CartMsg"] = "All wishlist items moved to cart.";
            // back to wishlist page
            return RedirectToAction(nameof(Index));
        }
    }
}
