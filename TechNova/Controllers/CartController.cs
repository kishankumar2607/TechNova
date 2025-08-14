using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Helpers;
using TechNova.Models;

namespace TechNova.Controllers
{
    // handles cart pages and checkout flow
    public class CartController : Controller
    {
        // database context
        private readonly AppDbContext context;
        // session key for the cart
        private const string CART_KEY = "CART";
        // sales tax rate
        private const decimal TAX_RATE = 0.13m;

        // inject database context
        public CartController(AppDbContext ctx) => context = ctx;

        // get cart from session (create empty if none)
        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        // save cart back to session
        private void SaveCart(List<CartItem> cart)
            => HttpContext.Session.SetObject(CART_KEY, cart);

        // show cart page with totals
        [HttpGet]
        public IActionResult Index()
        {
            // read cart
            var cart = GetCart();
            // compute subtotal, tax, shipping, total
            var (subtotal, tax, shipping, total) = CalcTotals(cart);

            // send totals to view
            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.TaxRate = TAX_RATE;
            ViewBag.Tax = tax;
            ViewBag.Total = total;

            // render cart view
            return View(cart);
        }

        // add a product to the cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, int qty = 1, string? returnUrl = null)
        {
            // keep quantity between 1 and 10
            qty = Math.Max(1, Math.Min(10, qty));

            // load product (no tracking)
            var p = context.Products.AsNoTracking().FirstOrDefault(x => x.ProductID == productId);
            // if not found, return 404
            if (p == null) return NotFound();

            // pick unit price (discounted if valid, else regular)
            var unit = (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0 && p.DiscountedPrice.HasValue)
                        ? p.DiscountedPrice.Value
                        : p.Price;

            // read cart and find existing item
            var cart = GetCart();
            var existing = cart.FirstOrDefault(x => x.ProductID == productId);

            // add new line or update existing
            if (existing == null)
            {
                // add new item
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
                // increase qty (still 1–10) and refresh price
                existing.Qty = Math.Max(1, Math.Min(10, existing.Qty + qty));
                existing.UnitPrice = unit;
            }

            // persist cart
            SaveCart(cart);

            // toast message for user
            TempData["CartMsg"] = $"{p.Name} added to cart.";

            // if safe return url provided, go back there
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // otherwise show cart
            return RedirectToAction(nameof(Index));
        }

        // update quantity for a product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int qty)
        {
            // read cart
            var cart = GetCart();
            // find item
            var item = cart.FirstOrDefault(x => x.ProductID == productId);
            if (item != null)
            {
                // set qty between 1 and 10
                item.Qty = Math.Max(1, Math.Min(10, qty));
                // save cart
                SaveCart(cart);
            }
            // back to cart
            return RedirectToAction(nameof(Index));
        }

        // remove a product from the cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            // read cart
            var cart = GetCart();
            // remove all lines for this product
            cart.RemoveAll(x => x.ProductID == productId);
            // save cart
            SaveCart(cart);
            // back to cart
            return RedirectToAction(nameof(Index));
        }

        // clear the entire cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            // save empty cart
            SaveCart(new List<CartItem>());
            // back to cart
            return RedirectToAction(nameof(Index));
        }

        // start checkout (decide payment flow)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Start(int paymentId)
        {
            // if not logged in, send to login and come back here
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(Start), "Cart", new { paymentId });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // 2 = COD, else bank checkout
            return (paymentId == 2) ? RedirectToAction(nameof(COD))
                                    : RedirectToAction(nameof(BankCheckout));
        }

        // show Cash on Delivery checkout page
        [HttpGet]
        public IActionResult COD()
        {
            // require login
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(COD), "Cart");
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // ensure cart has items
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            // compute totals
            var (subtotal, tax, shipping, total) = CalcTotals(cart);

            // prepare view model
            var vm = new CheckoutViewModel
            {
                Subtotal = subtotal,
                TaxRate = TAX_RATE,
                Tax = tax,
                Shipping = shipping,
                Total = total,
                Country = "Canada",
                State = "Ontario",
                SelectedPaymentID = 2,
                PaymentMethods = new()
            };

            // pass items for review
            ViewBag.CartItems = cart;
            // render COD view
            return View(vm);
        }

        // submit Cash on Delivery checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult COD(CheckoutViewModel vm)
        {
            // require login (and return here after)
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(COD), "Cart");
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            // read user id from cookie
            if (!int.TryParse(Request.Cookies["UserId"], out var customerId) || customerId <= 0)
                return RedirectToAction("Login", "Account");

            // ensure cart has items
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            // recompute totals server-side
            var (subtotal, tax, shipping, total) = CalcTotals(cart);
            vm.Subtotal = subtotal;
            vm.TaxRate = TAX_RATE;
            vm.Tax = tax;
            vm.Shipping = shipping;
            vm.Total = total;
            vm.SelectedPaymentID = 2;

            // if form invalid, show again with items
            if (!ModelState.IsValid)
            {
                ViewBag.CartItems = cart;
                return View(vm);
            }

            // save order and clear cart
            var orderId = SaveOrderFromCart(cart, customerId, paymentId: 2, billing: vm);
            SaveCart(new List<CartItem>());

            // go to success page
            return RedirectToAction("Success", "Orders", new { id = orderId });
        }

        // show Bank transfer checkout page
        [HttpGet]
        public IActionResult BankCheckout()
        {
            // require login
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(BankCheckout), "Cart");
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // ensure cart has items
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            // compute totals
            var (subtotal, tax, shipping, total) = CalcTotals(cart);

            // prepare view model
            var vm = new CheckoutViewModel
            {
                Subtotal = subtotal,
                TaxRate = TAX_RATE,
                Tax = tax,
                Shipping = shipping,
                Total = total,
                Country = "Canada",
                State = "Ontario",
                SelectedPaymentID = 1,
                PaymentMethods = new()
            };

            // pass items for review
            ViewBag.CartItems = cart;
            // render bank checkout view
            return View(vm);
        }

        // submit Bank transfer checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BankCheckout(CheckoutViewModel vm)
        {
            // require login (and return here after)
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(BankCheckout), "Cart");
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            // read user id from cookie
            if (!int.TryParse(Request.Cookies["UserId"], out var customerId) || customerId <= 0)
                return RedirectToAction("Login", "Account");

            // ensure cart has items
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            // recompute totals server-side
            var (subtotal, tax, shipping, total) = CalcTotals(cart);
            vm.Subtotal = subtotal;
            vm.TaxRate = TAX_RATE;
            vm.Tax = tax;
            vm.Shipping = shipping;
            vm.Total = total;
            vm.SelectedPaymentID = 1;

            // if form invalid, show again with items
            if (!ModelState.IsValid)
            {
                ViewBag.CartItems = cart;
                return View(vm);
            }

            // save order and clear cart
            var orderId = SaveOrderFromCart(cart, customerId, paymentId: 1, billing: vm);
            SaveCart(new List<CartItem>());

            // go to bank details page
            return RedirectToAction("BankDetails", "Orders", new { id = orderId });
        }

        // compute subtotal, tax, shipping, and total
        private (decimal subtotal, decimal tax, decimal shipping, decimal total) CalcTotals(List<CartItem> cart)
        {
            // sum of line totals
            var subtotal = cart.Sum(i => i.LineTotal);
            // free shipping over 500, else 30 (0 if empty cart)
            var shipping = subtotal > 500m ? 0m : (subtotal == 0 ? 0m : 30m);
            // round tax to 2 decimals
            var tax = Math.Round(subtotal * TAX_RATE, 2, MidpointRounding.AwayFromZero);
            // return all totals
            return (subtotal, tax, shipping, subtotal + tax + shipping);
        }

        // create order and order items from the cart
        private int SaveOrderFromCart(List<CartItem> cart, int customerId, int paymentId, CheckoutViewModel? billing = null)
        {
            // compute totals again for safety
            var totals = CalcTotals(cart);

            // build order record
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
            // save order
            context.Orders.Add(order);
            context.SaveChanges();

            // add each item for the order
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
            // save items
            context.SaveChanges();

            // return new order id
            return order.OrderID;
        }
    }
}
