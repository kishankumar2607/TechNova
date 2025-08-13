namespace TechNova.Models
{
    public class WishlistItem
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = "";
        public string? ImageURL { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public decimal? DiscountPercent { get; set; }

        public decimal EffectivePrice => DiscountedPrice ?? Price;
    }
}
