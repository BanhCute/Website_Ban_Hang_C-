using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebsiteBanHang.Data;
using WebsiteBanHang.Interfaces;
using WebsiteBanHang.Models;
using WebsiteBanHang.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace WebsiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context,
            ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                var categories = await _context.Categories.ToListAsync();
                if (!categories.Any())
                {
                    TempData["ErrorMessage"] = "Không có danh mục nào. Vui lòng thêm danh mục trước.";
                    return RedirectToAction("Index");
                }

                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(new Product());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Add GET: {ex.Message}");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile? imageFile)
        {
            try
            {
                _logger.LogInformation($"Received product data: Name={product.Name}, Price={product.Price}, CategoryId={product.CategoryId}, Quantity={product.Quantity}");

                // Kiểm tra CategoryId có tồn tại không
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
                if (!categoryExists)
                {
                    ModelState.AddModelError("CategoryId", "Danh mục không tồn tại");
                }

                if (ModelState.IsValid)
                {
                    // Xử lý ảnh nếu có
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                        
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        var filePath = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        product.ImageUrl = $"/images/products/{fileName}";
                    }

                    await _context.Products.AddAsync(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }

                // Log lỗi ModelState
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding product: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message);
            }

            // Nếu có lỗi, load lại danh mục và trả về view
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Product product, IFormFile? imageFile, List<IFormFile>? imageFiles, List<string>? imagesToDelete)
        {
            // Thêm logging để kiểm tra dữ liệu đầu vào
            _logger.LogInformation($"Received update request for product {product.Id}");
            _logger.LogInformation($"Product data: Name={product.Name}, Price={product.Price}, Quantity={product.Quantity}");

            if (!ModelState.IsValid)
            {
                // Log lỗi ModelState
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogError($"ModelState error: {error.ErrorMessage}");
                    }
                }
            }

            try
            {
                var existingProduct = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == product.Id);

                if (existingProduct == null)
                {
                    _logger.LogError($"Product with ID {product.Id} not found");
                    return NotFound();
                }

                // Log giá trị cũ
                _logger.LogInformation($"Current quantity: {existingProduct.Quantity}");

                // Cập nhật thông tin
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Quantity = product.Quantity; // Đảm bảo dòng này được thực thi
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;

                // Log giá trị mới trước khi lưu
                _logger.LogInformation($"New quantity to be saved: {existingProduct.Quantity}");

                // Đánh dấu entity đã được thay đổi
                _context.Entry(existingProduct).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                // Log sau khi lưu
                _logger.LogInformation($"Product updated successfully. New quantity: {existingProduct.Quantity}");

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating product: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
            }

            // Nếu có lỗi, load lại danh sách categories và trả về view
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View(product);
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            string uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return $"/images/{uniqueFileName}";
        }

        private void DeleteImageFile(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl)) return;

                // Lấy tên file từ URL
                string fileName = Path.GetFileName(imageUrl);
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                System.Diagnostics.Debug.WriteLine($"Error deleting image: {ex.Message}");
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Tìm tất cả OrderDetails liên quan đến sản phẩm
                var orderDetails = await _context.OrderDetails
                    .Where(od => od.ProductId == id)
                    .ToListAsync();

                // Xóa các OrderDetails
                if (orderDetails.Any())
                {
                    _context.OrderDetails.RemoveRange(orderDetails);
                    await _context.SaveChangesAsync();
                }

                // Tìm và xóa sản phẩm
                var product = await _context.Products.FindAsync(id);
                if (product != null)
                {
                    // Xóa các file ảnh
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        var imagePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            product.ImageUrl.TrimStart('/'));
                        
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    // Xóa các ảnh phụ
                    if (product.ImageUrls != null)
                    {
                        foreach (var imageUrl in product.ImageUrls)
                        {
                            var imagePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                imageUrl.TrimStart('/'));
                            
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }
                    }

                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Xóa sản phẩm thành công!";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
