using System;
using System.ComponentModel.DataAnnotations;

namespace WebShop.Models
{
    public class ProductChatMessage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsAdminReply { get; set; }
        public Product? Product { get; set; }
        public ApplicationUser? User { get; set; }
    }
}