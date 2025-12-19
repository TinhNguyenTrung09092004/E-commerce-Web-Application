using System.Collections.Generic;
using WebShop.Models;

namespace WebShop.Models
{
    public class HomeIndexViewModel
    {
        public List<Banner>? Banners { get; set; }
        public List<Product>? FeaturedProducts { get; set; }
        public List<Category>? Categories { get; set; }
        public List<Product>? NewProducts { get; set; }
    }
}