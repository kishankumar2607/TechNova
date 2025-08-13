using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Models;

namespace TechNova.Controllers
{
    // handles single-product checkout and order flow
    public class OrdersController : Controller
    {
        // database context
        private AppDbContext context { get; set; }
        // sales tax rate
        private const decimal TAX_RATE = 0.13m;

        // inject DbContext
        public OrdersController(AppDbContext ctx)
        {
            context = ctx;
        }

        // GET: show checkout page for one product
        [HttpGet]
        public IActionResult Checkout(int id, int qty = 1, int? paymentId = null)
        {
            // require login via cookie; if not, redirect to login and come back here
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action("Checkout", "Orders", new { id, qty = Clamp(qty) });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // load product by id (no tracking)
            var product = context.Products.AsNoTracking().FirstOrDefault(p => p.ProductID == id);
            // if not found, 404
            if (product == null) return NotFound();

            // pick effective unit price (discounted if valid, else regular)
            var unitPrice = (product.DiscountPercent.HasValue && product.DiscountPercent.Value > 0 && product.DiscountedPrice.HasValue)
                ? product.DiscountedPrice.Value
                : product.Price;

            // build checkout view model with defaults
            var vm = new CheckoutViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.Name,
                ProductImage = product.ImageURL,
                Qty = Clamp(qty),
                UnitPrice = unitPrice,
                ShippingLabel = "Free",
                Country = "Canada",
                State = "Ontario",
                PaymentMethods = new List<PaymentMethodVM>
                {
                    new() { PaymentID = 1, MethodName = "Bank" },
                    new() { PaymentID = 2, MethodName = "Cash on delivery" }
                },
                SelectedPaymentID = (paymentId == 1 || paymentId == 2) ? paymentId.Value : 1
            };

            // set tax and compute totals
            vm.TaxRate = TAX_RATE;
            vm.RecalculateTotals();
            // render checkout view
            return View(vm);
        }

        // POST: submit checkout form for one product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutViewModel vm)
        {
            // require login; if missing, send to login and return here
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action("Checkout", "Orders", new { id = vm.ProductID, qty = Clamp(vm.Qty) });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // normalize qty and set country/tax
            vm.Qty = Clamp(vm.Qty);
            vm.Country = "Canada";
            vm.TaxRate = TAX_RATE;

            // ensure payment methods exist on postback
            vm.PaymentMethods ??= new List<PaymentMethodVM>
            {
                new() { PaymentID = 1, MethodName = "Bank" },
                new() { PaymentID = 2, MethodName = "Cash on delivery" }
            };

            // recompute totals
            vm.RecalculateTotals();

            // if form invalid, show same page
            if (!ModelState.IsValid) return View(vm);

            // get customer id from cookie; if invalid, force login
            if (!int.TryParse(Request.Cookies["UserId"], out var customerId) || customerId <= 0)
            {
                var returnUrl = Url.Action("Checkout", "Orders", new { id = vm.ProductID, qty = vm.Qty });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // create order record
            var order = new Order
            {
                CustomerID = customerId,
                BillingName = vm.FullName,
                CompanyName = vm.CompanyName ?? string.Empty,
                StreetAddress = vm.StreetAddress,
                Apartment = vm.Apartment ?? string.Empty,
                City = vm.City,
                State = vm.State,
                PostalCode = vm.PostalCode,
                Country = vm.Country,
                PhoneNumber = vm.PhoneNumber,
                EmailAddress = vm.EmailAddress,
                PaymentID = vm.SelectedPaymentID,
                TotalAmount = vm.Total
            };
            // save order
            context.Orders.Add(order);
            context.SaveChanges();

            // add single order item
            var item = new OrderItem
            {
                OrderID = order.OrderID,
                ProductID = vm.ProductID,
                Quantity = vm.Qty,
                UnitPrice = vm.UnitPrice
            };
            // save item
            context.OrderItems.Add(item);
            context.SaveChanges();

            // if bank, go to bank details; else show success
            if (vm.SelectedPaymentID == 1)
                return RedirectToAction(nameof(BankDetails), new { id = order.OrderID });

            return RedirectToAction(nameof(Success), new { id = order.OrderID });
        }

        // GET: show bank transfer details for an order
        [HttpGet]
        public IActionResult BankDetails(int id)
        {
            // require login; if missing, go to login and return here
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(BankDetails), "Orders", new { id });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            // parse user id; if bad, force login
            if (!int.TryParse(Request.Cookies["UserId"], out var userId)) return RedirectToAction("Login", "Account");

            // load order (no tracking)
            var order = context.Orders.AsNoTracking().FirstOrDefault(o => o.OrderID == id);
            // if not found, 404
            if (order == null) return NotFound();

            // check admin or owner
            var isAdmin = string.Equals(Request.Cookies["Role"], "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && order.CustomerID != userId) return Forbid();

            // if not bank payment, go to success
            if (order.PaymentID != 1) return RedirectToAction(nameof(Success), new { id });

            // build VM with amount
            var vm = new BankDetailsViewModel
            {
                OrderID = id,
                Amount = order.TotalAmount
            };
            // show bank details view
            return View(vm);
        }

        // POST: submit bank details (mock confirm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BankDetails(BankDetailsViewModel vm)
        {
            // if invalid form, show again
            if (!ModelState.IsValid) return View(vm);

            // require login and return here
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(BankDetails), "Orders", new { id = vm.OrderID });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            // parse user id; if bad, force login
            if (!int.TryParse(Request.Cookies["UserId"], out var userId)) return RedirectToAction("Login", "Account");

            // load order
            var order = context.Orders.FirstOrDefault(o => o.OrderID == vm.OrderID);
            // if missing, 404
            if (order == null) return NotFound();

            // check admin or owner
            var isAdmin = string.Equals(Request.Cookies["Role"], "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && order.CustomerID != userId) return Forbid();

            // pretend success; redirect to success page
            return RedirectToAction(nameof(Success), new { id = vm.OrderID });
        }

        // show order success page (with human-friendly number)
        public IActionResult Success(int? id)
        {
            // reuse display number if already generated this session
            var existing = HttpContext.Session.GetString("LastOrderNo");
            if (!string.IsNullOrEmpty(existing))
            {
                ViewBag.DisplayOrderNo = existing;
                return View();
            }

            // generate 8-digit display number
            var display = System.Security.Cryptography.RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString("D8");

            // store and send to view
            HttpContext.Session.SetString("LastOrderNo", display);
            ViewBag.DisplayOrderNo = display;
            return View();
        }

        // helper: clamp quantity between 1 and 10
        private static int Clamp(int q) => Math.Max(1, Math.Min(10, q));
    }
}
