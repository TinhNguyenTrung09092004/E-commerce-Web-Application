using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models;
using X.PagedList;
using System.ComponentModel.DataAnnotations;

namespace WebShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexViewModel
            {
                Banners = await _context.Banners.ToListAsync(),
                FeaturedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Reviews)
                    .Where(p => p.Featured)
                    .Take(6)
                    .ToListAsync(),
                Categories = await _context.Categories
                    .Take(6)
                    .ToListAsync(),
                NewProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Reviews)
                    .OrderByDescending(p => p.Id)
                    .Take(6)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Products(string searchString, decimal? minPrice, decimal? maxPrice, int? page, string viewMode)
        {
            var pageNumber = page ?? 1;
            var pageSize = 4;

            ViewBag.ViewMode = viewMode ?? "grid";

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString));
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }

            var productList = await products.ToListAsync();
            var pagedProducts = productList.ToPagedList(pageNumber, pageSize);

            ViewBag.SearchString = searchString;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(pagedProducts);
        }

        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .Include(p => p.ChatMessages)
                .ThenInclude(cm => cm.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductDetailsViewModel
            {
                Product = product,
                Reviews = product.Reviews?.OrderByDescending(r => r.CreatedAt).ToList() ?? new List<ProductReview>(),
                ChatMessages = product.ChatMessages?.OrderByDescending(cm => cm.CreatedAt).ToList() ?? new List<ProductChatMessage>()
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddReview(int productId, string comment, int rating)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(comment) || comment.Length > 500)
            {
                ModelState.AddModelError("comment", "Comment is required and must not exceed 500 characters.");
            }

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("rating", "Rating must be between 1 and 5.");
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction("ProductDetails", new { id = productId });
            }

            var existingReview = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == user.Id);

            if (existingReview != null)
            {
                existingReview.Comment = comment;
                existingReview.Rating = rating;
                existingReview.CreatedAt = DateTime.Now;

                _context.ProductReviews.Update(existingReview);
                TempData["ReviewUpdated"] = true;
            }
            else
            {
                var review = new ProductReview
                {
                    ProductId = productId,
                    UserId = user.Id,
                    Comment = comment,
                    Rating = rating,
                    CreatedAt = DateTime.Now
                };

                _context.ProductReviews.Add(review);
                TempData["ReviewUpdated"] = false;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ProductDetails", new { id = productId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendChatMessage(int productId, string message)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
            {
                ModelState.AddModelError("message", "Message is required and must not exceed 500 characters.");
                return RedirectToAction("ProductDetails", new { id = productId });
            }

            var chatMessage = new ProductChatMessage
            {
                ProductId = productId,
                UserId = user.Id,
                Message = message,
                CreatedAt = DateTime.Now,
                IsAdminReply = false
            };

            _context.ProductChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            return RedirectToAction("ProductDetails", new { id = productId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (quantity <= 0)
            {
                ModelState.AddModelError("quantity", "Quantity must be greater than 0.");
                return RedirectToAction("ProductDetails", new { id = productId });
            }

            if (quantity > product.Stock)
            {
                ModelState.AddModelError("quantity", $"Only {product.Stock} items available in stock.");
                return RedirectToAction("ProductDetails", new { id = productId });
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == productId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Price = product.Price,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                if (cartItem.Quantity + quantity > product.Stock)
                {
                    ModelState.AddModelError("quantity", $"Only {product.Stock} items available in stock.");
                    return RedirectToAction("ProductDetails", new { id = productId });
                }
                cartItem.Quantity += quantity;
                cartItem.Price = product.Price;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Carts");
        }

        [Authorize]
        public async Task<IActionResult> Carts()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            return View(cartItems);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, int quantity)
        {
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId);
            if (cartItem == null)
            {
                return NotFound();
            }

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                if (quantity > cartItem.Product.Stock)
                {
                    ModelState.AddModelError("quantity", $"Only {cartItem.Product.Stock} items available in stock.");
                    return RedirectToAction("Carts");
                }
                cartItem.Quantity = quantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Carts");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveCartItem(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Carts");
        }

        [Authorize]
        public async Task<IActionResult> Checkout(string? voucherCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            decimal subtotal = cartItems.Sum(item => item.Price * item.Quantity);
            decimal discount = 0;
            string voucherMessage = null;

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive && v.StartDate <= DateTime.Now && v.EndDate >= DateTime.Now);
                if (voucher != null)
                {
                    discount = subtotal * (voucher.DiscountPercentage / 100);
                    voucherMessage = $"Voucher {voucherCode} applied. Discount: {voucher.DiscountPercentage}%";
                }
                else
                {
                    voucherMessage = "Invalid or expired voucher code.";
                }
            }

            decimal total = Math.Max(0, subtotal - discount);

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                ShippingAddress = user.Address,
                Subtotal = subtotal,
                Total = total,
                VoucherCode = voucherCode,
                VoucherMessage = voucherMessage,
                Discount = discount
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string? voucherCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(shippingAddress) || shippingAddress.Length > 200)
            {
                ModelState.AddModelError("shippingAddress", "Shipping address is required and must not exceed 200 characters.");
                return RedirectToAction("Checkout", new { voucherCode });
            }

            var cartItems = await _context.CartItems
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    c.UserId,
                    c.ProductId,
                    c.Price,
                    c.Quantity,
                    ProductName = _context.Products.Where(p => p.Id == c.ProductId).Select(p => p.Name).FirstOrDefault(),
                    ProductStock = _context.Products.Where(p => p.Id == c.ProductId).Select(p => p.Stock).FirstOrDefault()
                })
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Carts");
            }

            decimal subtotal = cartItems.Sum(item => item.Price * item.Quantity);
            decimal discount = 0;

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive && v.StartDate <= DateTime.Now && v.EndDate >= DateTime.Now);
                if (voucher != null)
                {
                    discount = subtotal * (voucher.DiscountPercentage / 100);
                }
                else
                {
                    return BadRequest("Invalid or expired voucher code.");
                }
            }

            foreach (var item in cartItems)
            {
                if (item.ProductStock < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for product {item.ProductName}");
                }
            }

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = "Placed",
                PaymentStatus = "Unpaid",
                ShippingAddress = shippingAddress,
                VoucherCode = string.IsNullOrEmpty(voucherCode) ? null : voucherCode,
                Subtotal = subtotal,
                Discount = discount,
                Items = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    ProductName = c.ProductName,
                    Price = c.Price,
                    Quantity = c.Quantity
                }).ToList()
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                    }
                }

                var cartItemsToRemove = await _context.CartItems
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();
                _context.CartItems.RemoveRange(cartItemsToRemove);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return RedirectToAction("Orders");
        }

        [Authorize]
        public async Task<IActionResult> Orders()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == user.Id)
                .ToListAsync();

            return View(orders);
        }

        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.Subtotal = order.Subtotal;
            ViewBag.Discount = order.Discount;
            ViewBag.Total = Math.Max(0, order.Subtotal - order.Discount);

            if (!string.IsNullOrEmpty(order.VoucherCode))
            {
                ViewBag.Voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == order.VoucherCode);
            }

            return View(order);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PayOrder(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            order.PaymentStatus = "Paid";
            order.Status = "Paid";
            await _context.SaveChangesAsync();

            return RedirectToAction("Orders");
        }

        [Authorize]
        [HttpPost]

        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in order.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Product with ID {item.ProductId} not found.");
                    }
                }

                order.Status = "Canceled";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw; 
            }

            return RedirectToAction("Orders");
        }
    
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new UpdateProfileViewModel
            {
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Dashboard(UpdateProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }

        public IActionResult CheckoutSuccess()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }

    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; }
        [Required]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Shipping address must be between 5 and 200 characters")]
        public string ShippingAddress { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string VoucherCode { get; set; }
        public string VoucherMessage { get; set; }
        public decimal Discount { get; set; }
    }

    public class ProductDetailsViewModel
    {
        public Product Product { get; set; }
        public List<ProductReview> Reviews { get; set; }
        public List<ProductChatMessage> ChatMessages { get; set; }
    }
}