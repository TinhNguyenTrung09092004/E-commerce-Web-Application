using System.ComponentModel.DataAnnotations;

namespace WebShop.Models
{
    public class Brand
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}