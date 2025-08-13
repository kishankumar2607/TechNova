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
            var (subtotal, tax, shipping, total) = CalcTotals(cart);

            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.TaxRate = TAX_RATE;
            ViewBag.Tax = tax;
            ViewBag.Total = total;

            return View(cart);
        }

        // POST /Cart/Add
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

        // Choose flow from cart (multi-item)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Start(int paymentId)
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(Start), "Cart", new { paymentId });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            return (paymentId == 2) ? RedirectToAction(nameof(COD))
                                    : RedirectToAction(nameof(BankCheckout));
        }

        // ---------- COD (multi-item) : collects delivery details ----------
        [HttpGet]
        public IActionResult COD()
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(COD), "Cart");
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            var (subtotal, tax, shipping, total) = CalcTotals(cart);

            var vm = new CheckoutViewModel
            {
                Subtotal = subtotal,
                TaxRate = TAX_RATE,
                Tax = tax,
                Shipping = shipping,
                Total = total,
                Country = "Canada",
                State = "Ontario",
                SelectedPaymentID = 2,   // COD
                PaymentMethods = new()
            };

            ViewBag.CartItems = cart;
            return View(vm); // Views/Cart/COD.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult COD(CheckoutViewModel vm)
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(COD), "Cart");
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            if (!int.TryParse(Request.Cookies["UserId"], out var customerId) || customerId <= 0)
                return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            // Recalc totals from server-side cart
            var (subtotal, tax, shipping, total) = CalcTotals(cart);
            vm.Subtotal = subtotal;
            vm.TaxRate = TAX_RATE;
            vm.Tax = tax;
            vm.Shipping = shipping;
            vm.Total = total;
            vm.SelectedPaymentID = 2;

            if (!ModelState.IsValid)
            {
                ViewBag.CartItems = cart;
                return View(vm);
            }

            var orderId = SaveOrderFromCart(cart, customerId, paymentId: 2, billing: vm);
            SaveCart(new List<CartItem>());

            return RedirectToAction("Success", "Orders", new { id = orderId });
        }

        // ---------- Bank (multi-item) : collects delivery details ----------
        [HttpGet]
        public IActionResult BankCheckout()
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(BankCheckout), "Cart");
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            var (subtotal, tax, shipping, total) = CalcTotals(cart);

            var vm = new CheckoutViewModel
            {
                Subtotal = subtotal,
                TaxRate = TAX_RATE,
                Tax = tax,
                Shipping = shipping,
                Total = total,
                Country = "Canada",
                State = "Ontario",
                SelectedPaymentID = 1,   // Bank
                PaymentMethods = new()
            };

            ViewBag.CartItems = cart;
            return View(vm); // Views/Cart/BankCheckout.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BankCheckout(CheckoutViewModel vm)
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(BankCheckout), "Cart");
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            if (!int.TryParse(Request.Cookies["UserId"], out var customerId) || customerId <= 0)
                return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            var (subtotal, tax, shipping, total) = CalcTotals(cart);
            vm.Subtotal = subtotal;
            vm.TaxRate = TAX_RATE;
            vm.Tax = tax;
            vm.Shipping = shipping;
            vm.Total = total;
            vm.SelectedPaymentID = 1;

            if (!ModelState.IsValid)
            {
                ViewBag.CartItems = cart;
                return View(vm);
            }

            var orderId = SaveOrderFromCart(cart, customerId, paymentId: 1, billing: vm);
            SaveCart(new List<CartItem>());

            return RedirectToAction("BankDetails", "Orders", new { id = orderId });
        }

        // ---------- helpers ----------
        private (decimal subtotal, decimal tax, decimal shipping, decimal total) CalcTotals(List<CartItem> cart)
        {
            var subtotal = cart.Sum(i => i.LineTotal);
            var shipping = subtotal > 500m ? 0m : (subtotal == 0 ? 0m : 30m);
            var tax = Math.Round(subtotal * TAX_RATE, 2, MidpointRounding.AwayFromZero);
            return (subtotal, tax, shipping, subtotal + tax + shipping);
        }

        private int SaveOrderFromCart(List<CartItem> cart, int customerId, int paymentId, CheckoutViewModel? billing = null)
        {
            var totals = CalcTotals(cart);

            var order = new Order
            {
                CustomerID = customerId,
                BillingName = billing?.FullName ?? "",
                CompanyName = billing?.CompanyName ?? "",
                StreetAddress = billing?.StreetAddress ?? "",
                Apartment = billing?.Apartment ?? "",
                City = billing?.City ?? "",
                State = billing?.State ?? "Ontario",
                PostalCode = billing?.PostalCode ?? "",
                Country = billing?.Country ?? "Canada",
                PhoneNumber = billing?.PhoneNumber ?? "",
                EmailAddress = billing?.EmailAddress ?? "",
                PaymentID = paymentId,  // 2 = COD, 1 = Bank
                TotalAmount = totals.total
            };
            context.Orders.Add(order);
            context.SaveChanges();

            foreach (var it in cart)
            {
                context.OrderItems.Add(new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = it.ProductID,
                    Quantity = it.Qty,
                    UnitPrice = it.UnitPrice
                });
            }
            context.SaveChanges();

            return order.OrderID;
        }
    }
}
