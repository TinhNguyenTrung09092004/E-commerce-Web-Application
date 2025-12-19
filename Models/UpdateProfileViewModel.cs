using System.ComponentModel.DataAnnotations;

namespace WebShop.Models
{
    public class UpdateProfileViewModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}