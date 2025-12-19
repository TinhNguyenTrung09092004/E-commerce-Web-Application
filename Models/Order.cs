using System.ComponentModel.DataAnnotations;

namespace WebShop.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public DateTime OrderDate { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? ShippingAddress { get; set; }
        public string? VoucherCode { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public List<OrderItem>? Items { get; set; }
    }
}