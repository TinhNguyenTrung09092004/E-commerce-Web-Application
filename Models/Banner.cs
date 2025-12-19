using System.ComponentModel.DataAnnotations;

namespace WebShop.Models
{
    public class Banner
    {
        public int Id { get; set; }
        [Required]
        public string? ImagePath { get; set; }
    }
}