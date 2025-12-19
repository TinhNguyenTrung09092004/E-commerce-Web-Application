using System.ComponentModel.DataAnnotations;

namespace WebShop.Models
{
    public class Voucher
    {
        public int Id { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Code can only contain letters and numbers")]
        public string? Code { get; set; }
        [Required]
        [Range(1, 100, ErrorMessage = "Discount percentage must be between 1 and 100")]
        public decimal DiscountPercentage { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        [DateGreaterThan("StartDate", ErrorMessage = "End date must be after start date")]
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _otherProperty;

        public DateGreaterThanAttribute(string otherProperty)
        {
            _otherProperty = otherProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var otherPropertyInfo = validationContext.ObjectType.GetProperty(_otherProperty);
            var otherValue = otherPropertyInfo?.GetValue(validationContext.ObjectInstance);

            if (value is DateTime endDate && otherValue is DateTime startDate)
            {
                if (endDate <= startDate)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}