using System.ComponentModel.DataAnnotations;

namespace TechNova.Models
{
    public class CheckoutViewModel
    {
        // Product summary
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string? ProductImage { get; set; }

        [Range(1, 10, ErrorMessage = "Quantity must be between 1 and 10.")]
        public int Qty { get; set; } = 1;

        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        public decimal UnitPrice { get; set; }

        // Shipping
        public string ShippingLabel { get; set; } = "Free";
        public decimal Shipping { get; set; } = 0m;

        // Totals
        public decimal Subtotal { get; set; }
        public decimal TaxRate { get; set; }   // e.g., 0.13m
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        public void RecalculateTotals()
        {
            var q = Math.Max(1, Math.Min(10, Qty));
            Subtotal = UnitPrice * q;

            // ✅ Shipping rule: Free if order (subtotal) > $500, else $30
            Shipping = Subtotal > 500m ? 0m : 30m;
            ShippingLabel = Shipping <= 0m ? "Free" : Shipping.ToString("C");

            // Tax then total
            Tax = Math.Round(Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
            Total = Subtotal + Tax + Shipping;
        }

        // Payment
        [Range(1, int.MaxValue, ErrorMessage = "Please select a payment method.")]
        public int SelectedPaymentID { get; set; } // 1=Bank, 2=COD
        public List<PaymentMethodVM> PaymentMethods { get; set; } = new();
        public string? CouponCode { get; set; }

        // Billing
        [Required] public string FullName { get; set; } = "";
        public string? CompanyName { get; set; }
        [Required] public string StreetAddress { get; set; } = "";
        public string? Apartment { get; set; }
        [Required] public string City { get; set; } = "";
        [Required] public string State { get; set; } = "Ontario";  // Province/Territory
        [Required] public string PostalCode { get; set; } = "";
        [Required] public string Country { get; set; } = "Canada";
        [Required] public string PhoneNumber { get; set; } = "";
        [Required, EmailAddress] public string EmailAddress { get; set; } = "";
        public bool SaveForNextTime { get; set; }
    }

    public class PaymentMethodVM
    {
        public int PaymentID { get; set; }
        public string MethodName { get; set; } = "";
    }
}