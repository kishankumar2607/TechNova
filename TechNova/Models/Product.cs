namespace TechNova.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQty { get; set; }
        public string? ImageURL { get; set; }
        public decimal AvgRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
