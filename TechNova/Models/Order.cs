using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        public int CustomerID { get; set; }

        public string BillingName { get; set; }
        public string CompanyName { get; set; }
        public string StreetAddress { get; set; }
        public string Apartment { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }

        public int PaymentID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public ICollection<OrderItem> Items { get; set; }
    }
}
