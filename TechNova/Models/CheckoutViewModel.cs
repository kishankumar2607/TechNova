using System.ComponentModel.DataAnnotations;

namespace TechNova.Models
{
    public class CheckoutViewModel
    {
        // product summary
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string? ProductImage { get; set; }

        [Range(1, 10, ErrorMessage = "Quantity must be between 1 and 10.")]
        public int Qty { get; set; } = 1;

        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")] // non-negative
        public decimal UnitPrice { get; set; }

        public string ShippingLabel { get; set; } = "Free";

        // totals
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; } = 0m;

        public decimal TaxRate { get; set; }   // e.g., 0.13m
        public decimal Tax { get; set; }

        public decimal Total { get; set; }

        public void RecalculateTotals()
        {
            var q = Math.Max(1, Math.Min(10, Qty));
            Subtotal = UnitPrice * q;
            Tax = Math.Round(Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
            Total = Subtotal + Tax + Shipping;
        }

        // payment
        // Payment
        [Range(1, int.MaxValue, ErrorMessage = "Please select a payment method.")]
        public int SelectedPaymentID { get; set; } // 1=Bank, 2=COD
        public List<PaymentMethodVM> PaymentMethods { get; set; } = new();
        public string? CouponCode { get; set; }

        // billing
        [Required] public string FullName { get; set; } = "";
        [Required] public string StreetAddress { get; set; } = "";
        public string? Apartment { get; set; }
        [Required] public string City { get; set; } = "";
        [Required] public string State { get; set; } = "Ontario";
        [Required] public string PostalCode { get; set; } = "";
        [Required] public string Country { get; set; } = "";
        [Required] public string PhoneNumber { get; set; } = "";
        [Required, EmailAddress] public string EmailAddress { get; set; } = "";
        public bool SaveForNextTime { get; set; }
        public string? CompanyName { get; set; }
    }

    public class PaymentMethodVM
    {
        public int PaymentID { get; set; }
        public string MethodName { get; set; } = "";
    }
}