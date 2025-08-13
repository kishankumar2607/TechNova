using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TechNova.Models
{
    // Domain model for a catalog product.
    // DataAnnotations drive validation; monetary/rate precision is enforced via attributes/DbContext.
    public class Product
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Enter a valid price")]
        public decimal Price { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        [Precision(5, 2)]
        public decimal? DiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        [Precision(18, 2)]
        public decimal? DiscountedPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQty { get; set; }

        [Required(ErrorMessage = "Image URL is required")]
        public string? ImageURL { get; set; }

        public decimal AvgRating { get; set; }
        public int ReviewCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
