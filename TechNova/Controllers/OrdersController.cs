using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova.Models;

namespace TechNova.Controllers
{
    public class OrdersController : Controller
    {
        private AppDbContext context { get; set; }

        public OrdersController(AppDbContext ctx)
        {
            context = ctx;
        }

        // GET /Orders/Checkout?id=123&qty=1  (from Buy Now)
        [HttpGet]
        public IActionResult Checkout(int id, int qty = 1)
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
                Qty = Math.Max(1, Math.Min(10, qty)),
                UnitPrice = unitPrice,
                ShippingLabel = "Free",
                Country = "Canada",          // ✅ default to Canada
                State = "Ontario",           // (optional default)
                PaymentMethods = new List<PaymentMethodVM>
                {
                    new() { PaymentID = 1, MethodName = "Bank" },
                    new() { PaymentID = 2, MethodName = "Cash on delivery" }
                },
                SelectedPaymentID = 2
            };

            vm.TaxRate = GetTaxRateForProvince(vm.State);
            vm.RecalculateTotals();
            return View(vm);
        }

        // POST /Orders/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutViewModel vm)
        {
            // 🔒 Require login (custom cookie)
            if (string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                var returnUrl = Url.Action("Checkout", "Orders", new { id = vm.ProductID, qty = Clamp(vm.Qty) });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // ensure qty 1..10 and totals correct
            vm.Qty = Clamp(vm.Qty);
            vm.Country = "Canada";
            vm.TaxRate = GetTaxRateForProvince(vm.State);

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
                State = vm.State,         // from dropdown
                PostalCode = vm.PostalCode,
                Country = vm.Country,       // "Canada"
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

            // 🔀 Branch by payment method
            if (vm.SelectedPaymentID == 1) // Bank
                return RedirectToAction(nameof(BankDetails), new { id = order.OrderID });

            // COD
            return RedirectToAction(nameof(Success), new { id = order.OrderID });
        }

        // GET /Orders/BankDetails/5
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

        // POST /Orders/BankDetails
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

            // 🔒 Keep it simple: we won't store bank details now.
            // If you want to persist later, we can add a table + migration.

            // All good → show success
            return RedirectToAction(nameof(Success), new { id = vm.OrderID });
        }

        public IActionResult Success(int id) => View(model: id);

        private static int Clamp(int q) => Math.Max(1, Math.Min(10, q));

        private static decimal GetTaxRateForProvince(string? state)
        {
            if (string.IsNullOrWhiteSpace(state)) return 0.13m; // default ON
            switch (state.Trim())
            {
                case "Alberta":
                case "Northwest Territories":
                case "Nunavut":
                case "Yukon": return 0.05m; // GST
                case "British Columbia": return 0.12m; // GST+PST
                case "Manitoba": return 0.12m; // GST+RST
                case "New Brunswick":
                case "Newfoundland and Labrador":
                case "Nova Scotia":
                case "Prince Edward Island": return 0.15m; // HST
                case "Ontario": return 0.13m; // HST
                case "Quebec": return 0.14975m; // GST+QST
                case "Saskatchewan": return 0.11m; // GST+PST
                default: return 0.13m;
            }
        }
    }
}