namespace TechNova.Models
{
    // Session-backed cart line item used by the Cart; stores snapshot pricing and quantity.
    // LineTotal is a computed value (UnitPrice * Qty).
    public class CartItem
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = "";
        public string? ImageURL { get; set; }
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
        public decimal LineTotal => UnitPrice * Qty;
    }
}
