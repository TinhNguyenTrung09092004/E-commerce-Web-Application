using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models;
using X.PagedList;
using System.ComponentModel.DataAnnotations;

namespace WebShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                NumberOfAdmins = await _userManager.GetUsersInRoleAsync("Admin").ContinueWith(t => t.Result.Count),
                NumberOfCustomers = await _userManager.GetUsersInRoleAsync("User").ContinueWith(t => t.Result.Count),
                TotalProducts = await _context.Products.CountAsync(),
                OrdersThisMonth = await _context.Orders
                    .Where(o => o.OrderDate.Year == DateTime.Now.Year && o.OrderDate.Month == DateTime.Now.Month && o.Status == "Delivered")
                    .CountAsync(),
                CategoryDistribution = await _context.Products
                    .Include(p => p.Category)
                    .GroupBy(p => p.Category.Name)
                    .ToDictionaryAsync(g => g.Key, g => g.Count()),
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= DateTime.Now.AddMonths(-12) && o.Status == "Delivered")
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .ToDictionaryAsync(
                        g => $"{g.Key.Year}-{g.Key.Month:00}",
                        g => g.Sum(o => Math.Max(0, o.Subtotal - o.Discount))
                    ),
                NewCustomersByMonth = await _context.Users
                    .Where(u => u.EmailConfirmed)
                    .GroupBy(u => new
                    {
                        Year = u.LockoutEnd.HasValue ? u.LockoutEnd.Value.Year : DateTime.Now.Year,
                        Month = u.LockoutEnd.HasValue ? u.LockoutEnd.Value.Month : DateTime.Now.Month
                    })
                    .ToDictionaryAsync(
                        g => $"{g.Key.Year}-{g.Key.Month:00}",
                        g => g.Count()
                    )
            };

            return View(model);
        }

        public async Task<IActionResult> Products(int? page)
        {
            var pageNumber = page ?? 1;
            var pageSize = 2;

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();

            var pagedProducts = products.ToPagedList(pageNumber, pageSize);
            return View(pagedProducts);
        }

        [HttpGet]
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

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Product model, IFormFile imageFile)
        {
            ModelState.Remove(nameof(Product.ImagePath));
            ModelState.Remove(nameof(Product.Reviews));
            ModelState.Remove(nameof(Product.ChatMessages));
            ModelState.Remove(nameof(Product.Category));
            ModelState.Remove(nameof(Product.Brand));

            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG, WEBP, or GIF files are allowed.");
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("imageFile", "Image file size must not exceed 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    model.ImagePath = "/images/" + fileName;
                }

                model.Reviews = new List<ProductReview>();
                model.ChatMessages = new List<ProductChatMessage>();

                _context.Products.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Products");
            }
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", model.BrandId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Product model, IFormFile imageFile)
        {
            ModelState.Remove(nameof(Product.ImagePath));
            ModelState.Remove(nameof(Product.Reviews));
            ModelState.Remove(nameof(Product.ChatMessages));
            ModelState.Remove(nameof(Product.Category));
            ModelState.Remove(nameof(Product.Brand));

            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG, or GIF files are allowed.");
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("imageFile", "Image file size must not exceed 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                var existingProduct = await _context.Products.FindAsync(model.Id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    existingProduct.ImagePath = "/images/" + fileName;
                }

                existingProduct.Name = model.Name;
                existingProduct.Price = model.Price;
                existingProduct.OldPrice = model.OldPrice;
                existingProduct.Stock = model.Stock;
                existingProduct.BrandId = model.BrandId;
                existingProduct.CategoryId = model.CategoryId;
                existingProduct.Featured = model.Featured;
                existingProduct.Description = model.Description;

                await _context.SaveChangesAsync();
                return RedirectToAction("Products");
            }
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", model.BrandId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Products");
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .ToListAsync();
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (status == "Shipped" || status == "Canceled" || status == "Delivered")
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Orders");
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(order.VoucherCode))
            {
                ViewBag.Voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == order.VoucherCode);
            }

            return View(order);
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == id)
            {
                return BadRequest("You cannot lock your own account.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            await _context.SaveChangesAsync();
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _context.SaveChangesAsync();
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Vouchers()
        {
            var vouchers = await _context.Vouchers.ToListAsync();
            return View(vouchers);
        }

        [HttpGet]
        public IActionResult CreateVoucher()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateVoucher(Voucher model)
        {
            if (ModelState.IsValid)
            {
                var existingVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == model.Code);
                if (existingVoucher != null)
                {
                    ModelState.AddModelError("Code", "Voucher code already exists.");
                    return View(model);
                }

                _context.Vouchers.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Vouchers");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditVoucher(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }
            return View(voucher);
        }

        [HttpPost]
        public async Task<IActionResult> EditVoucher(Voucher model)
        {
            if (ModelState.IsValid)
            {
                var existingVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == model.Code && v.Id != model.Id);
                if (existingVoucher != null)
                {
                    ModelState.AddModelError("Code", "Voucher code already exists.");
                    return View(model);
                }

                var voucher = await _context.Vouchers.FindAsync(model.Id);
                if (voucher == null)
                {
                    return NotFound();
                }

                voucher.Code = model.Code;
                voucher.DiscountPercentage = model.DiscountPercentage;
                voucher.StartDate = model.StartDate;
                voucher.EndDate = model.EndDate;
                voucher.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                return RedirectToAction("Vouchers");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Vouchers");
        }

        public async Task<IActionResult> Banners()
        {
            var banners = await _context.Banners.ToListAsync();
            return View(banners);
        }

        [HttpGet]
        public IActionResult CreateBanner()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBanner(Banner model, IFormFile imageFile)
        {
            ModelState.Remove(nameof(Banner.ImagePath));
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Image file is required.");
            }
            else
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG, WEBP, or GIF files are allowed.");
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("imageFile", "Image file size must not exceed 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                model.ImagePath = "/images/" + fileName;

                _context.Banners.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Banners");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditBanner(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return NotFound();
            }
            return View(banner);
        }

        [HttpPost]
        public async Task<IActionResult> EditBanner(Banner model, IFormFile imageFile)
        {
            ModelState.Remove(nameof(Banner.ImagePath));
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG, or GIF files are allowed.");
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("imageFile", "Image file size must not exceed 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                var existingBanner = await _context.Banners.FindAsync(model.Id);
                if (existingBanner == null)
                {
                    return NotFound();
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    existingBanner.ImagePath = "/images/" + fileName;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Banners");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBanner(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Banners");
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(Category model, IFormFile imageFile)
        {
            ModelState.Remove(nameof(Category.ImagePath));
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG, or GIF files are allowed.");
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("imageFile", "Image file size must not exceed 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    model.ImagePath = "/images/" + fileName;
                }

                _context.Categories.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Categories");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(Category model, IFormFile imageFile)
        {
            ModelState.Remove(nameof(Category.ImagePath));
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG, or GIF files are allowed.");
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("imageFile", "Image file size must not exceed 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                var existingCategory = await _context.Categories.FindAsync(model.Id);
                if (existingCategory == null)
                {
                    return NotFound();
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    existingCategory.ImagePath = "/images/" + fileName;
                }

                existingCategory.Name = model.Name;
                existingCategory.Description = model.Description;

                await _context.SaveChangesAsync();
                return RedirectToAction("Categories");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Categories");
        }

        public async Task<IActionResult> Brands()
        {
            var brands = await _context.Brands.ToListAsync();
            return View(brands);
        }

        [HttpGet]
        public IActionResult CreateBrand()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBrand(Brand model)
        {
            if (ModelState.IsValid)
            {
                var existingBrand = await _context.Brands.FirstOrDefaultAsync(b => b.Name == model.Name);
                if (existingBrand != null)
                {
                    ModelState.AddModelError("Name", "Brand name already exists.");
                    return View(model);
                }

                _context.Brands.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Brands");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        [HttpPost]
        public async Task<IActionResult> EditBrand(Brand model)
        {
            if (ModelState.IsValid)
            {
                var existingBrand = await _context.Brands.FirstOrDefaultAsync(b => b.Name == model.Name && b.Id != model.Id);
                if (existingBrand != null)
                {
                    ModelState.AddModelError("Name", "Brand name already exists.");
                    return View(model);
                }

                var brand = await _context.Brands.FindAsync(model.Id);
                if (brand == null)
                {
                    return NotFound();
                }

                brand.Name = model.Name;
                brand.Description = model.Description;

                await _context.SaveChangesAsync();
                return RedirectToAction("Brands");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Brands");
        }

        public async Task<IActionResult> Notifications()
        {
            var latestMessages = await _context.ProductChatMessages
                .Include(cm => cm.Product)
                .Include(cm => cm.User)
                .OrderByDescending(cm => cm.CreatedAt)
                .ToListAsync();

            var distinctMessages = latestMessages
                .GroupBy(cm => cm.ProductId)
                .Select(g => g.First())
                .OrderByDescending(cm => cm.CreatedAt)
                .ToList();

            return View(distinctMessages);
        }

        [HttpGet]
        public async Task<IActionResult> ReplyChatMessage(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var messages = await _context.ProductChatMessages
                .Include(cm => cm.User)
                .Where(cm => cm.ProductId == productId)
                .OrderBy(cm => cm.CreatedAt)
                .ToListAsync();

            var model = new ReplyChatMessageViewModel
            {
                ProductId = productId,
                ProductName = product.Name,
                Messages = messages,
                ReplyMessage = string.Empty
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ReplyChatMessage(ReplyChatMessageViewModel model)
        {
            ModelState.Remove(nameof(ReplyChatMessageViewModel.ProductName));
            ModelState.Remove(nameof(ReplyChatMessageViewModel.Messages));

            if (ModelState.IsValid)
            {
                var admin = await _userManager.GetUserAsync(User);
                if (admin == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                {
                    return NotFound();
                }

                var chatMessage = new ProductChatMessage
                {
                    ProductId = model.ProductId,
                    UserId = admin.Id,
                    Message = model.ReplyMessage,
                    CreatedAt = DateTime.Now,
                    IsAdminReply = true
                };

                _context.ProductChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                return RedirectToAction("Notifications");
            }

            var productForModel = await _context.Products.FindAsync(model.ProductId);
            if (productForModel == null)
            {
                return NotFound();
            }

            model.ProductName = productForModel.Name;
            model.Messages = await _context.ProductChatMessages
                .Include(cm => cm.User)
                .Where(cm => cm.ProductId == model.ProductId)
                .OrderBy(cm => cm.CreatedAt)
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    return RedirectToAction("ChangePasswordSuccess", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var user = await _userManager.GetUserAsync(User);
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

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("UpdateProfileSuccess", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }

    public class ReplyChatMessageViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public List<ProductChatMessage> Messages { get; set; }
        [Required]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters")]
        public string ReplyMessage { get; set; }
    }
}