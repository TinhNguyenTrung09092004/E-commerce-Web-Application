using System;
using System.ComponentModel.DataAnnotations;

namespace WebShop.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Comment { get; set; } = string.Empty;
        [Range(1, 5)]
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public Product? Product { get; set; } 
        public ApplicationUser? User { get; set; }
    }
}