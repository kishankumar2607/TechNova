using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Models;

namespace TechNova.Controllers
{
    public class OrdersController : Controller
    {
        private AppDbContext context { get; set; }
        private const decimal TAX_RATE = 0.13m;

        public OrdersController(AppDbContext ctx)
        {
            context = ctx;
        }

        [HttpGet]
        public IActionResult Checkout(int id, int qty = 1, int? paymentId = null)
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action("Checkout", "Orders", new { id, qty = Clamp(qty) });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            var product = context.Products.AsNoTracking().FirstOrDefault(p => p.ProductID == id);
            if (product == null) return NotFound();

            var unitPrice = (product.DiscountPercent.HasValue && product.DiscountPercent.Value > 0 && product.DiscountedPrice.HasValue)
                ? product.DiscountedPrice.Value
                : product.Price;

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

            vm.TaxRate = TAX_RATE;
            vm.RecalculateTotals();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutViewModel vm)
        {
            // Require login (custom cookie)
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action("Checkout", "Orders", new { id = vm.ProductID, qty = Clamp(vm.Qty) });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // ensure qty 1..10 and totals correct
            vm.Qty = Clamp(vm.Qty);
            vm.Country = "Canada";
            vm.TaxRate = TAX_RATE;

            // re-populate methods on postback
            vm.PaymentMethods ??= new List<PaymentMethodVM>
            {
                new() { PaymentID = 1, MethodName = "Bank" },
                new() { PaymentID = 2, MethodName = "Cash on delivery" }
            };

            vm.RecalculateTotals();

            if (!ModelState.IsValid) return View(vm);

            if (!int.TryParse(Request.Cookies["UserId"], out var customerId) || customerId <= 0)
            {
                var returnUrl = Url.Action("Checkout", "Orders", new { id = vm.ProductID, qty = vm.Qty });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

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
            context.Orders.Add(order);
            context.SaveChanges();

            var item = new OrderItem
            {
                OrderID = order.OrderID,
                ProductID = vm.ProductID,
                Quantity = vm.Qty,
                UnitPrice = vm.UnitPrice
            };
            context.OrderItems.Add(item);
            context.SaveChanges();

            if (vm.SelectedPaymentID == 1)
                return RedirectToAction(nameof(BankDetails), new { id = order.OrderID });

            return RedirectToAction(nameof(Success), new { id = order.OrderID });
        }

        [HttpGet]
        public IActionResult BankDetails(int id)
        {
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(BankDetails), "Orders", new { id });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            if (!int.TryParse(Request.Cookies["UserId"], out var userId)) return RedirectToAction("Login", "Account");

            var order = context.Orders.AsNoTracking().FirstOrDefault(o => o.OrderID == id);
            if (order == null) return NotFound();

            var isAdmin = string.Equals(Request.Cookies["Role"], "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && order.CustomerID != userId) return Forbid();

            // Optional: ensure they actually chose Bank
            if (order.PaymentID != 1) return RedirectToAction(nameof(Success), new { id });

            var vm = new BankDetailsViewModel
            {
                OrderID = id,
                Amount = order.TotalAmount
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BankDetails(BankDetailsViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action(nameof(BankDetails), "Orders", new { id = vm.OrderID });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            if (!int.TryParse(Request.Cookies["UserId"], out var userId)) return RedirectToAction("Login", "Account");

            var order = context.Orders.FirstOrDefault(o => o.OrderID == vm.OrderID);
            if (order == null) return NotFound();

            var isAdmin = string.Equals(Request.Cookies["Role"], "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && order.CustomerID != userId) return Forbid();

            return RedirectToAction(nameof(Success), new { id = vm.OrderID });
        }

        public IActionResult Success(int? id)
        {
            var existing = HttpContext.Session.GetString("LastOrderNo");
            if (!string.IsNullOrEmpty(existing))
            {
                ViewBag.DisplayOrderNo = existing;
                return View();
            }

            var display = System.Security.Cryptography.RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString("D8");

            HttpContext.Session.SetString("LastOrderNo", display);
            ViewBag.DisplayOrderNo = display;
            return View();
        }

        private static int Clamp(int q) => Math.Max(1, Math.Min(10, q));
    }
}