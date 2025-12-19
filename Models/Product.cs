using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebShop.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
        public string Name { get; set; } = string.Empty;
        [Required]
        [Range(0.01, 1000000, ErrorMessage = "Price must be between 0.01 and 1,000,000")]
        public decimal Price { get; set; }
        [Range(0, 1000000, ErrorMessage = "Old price must be between 0 and 1,000,000")]
        public decimal? OldPrice { get; set; }
        [Required]
        [Range(0, 10000, ErrorMessage = "Stock must be between 0 and 10,000")]
        public int Stock { get; set; }
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public bool Featured { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        [Required]
        public int BrandId { get; set; }
        public Brand? Brand { get; set; }
        public List<ProductReview>? Reviews { get; set; }
        public List<ProductChatMessage>? ChatMessages { get; set; }
    }
}