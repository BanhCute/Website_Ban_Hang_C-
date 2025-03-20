using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebsiteBanHang.Data;
using WebsiteBanHang.Interfaces;
using WebsiteBanHang.Models;
using WebsiteBanHang.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace WebsiteBanHang.Controllers
{
   
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        public IActionResult Add()
        {
            // Lấy danh sách category để hiển thị trong dropdown
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind("Name,Price,Description,CategoryId")] Product product, List<IFormFile> imageFiles)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload nhiều ảnh
                    if (imageFiles != null && imageFiles.Any())
                    {
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        // Lưu ảnh đầu tiên làm ảnh chính
                        var firstImage = imageFiles.First();
                        var firstFileName = Guid.NewGuid().ToString() + Path.GetExtension(firstImage.FileName);
                        var firstFilePath = Path.Combine(uploadPath, firstFileName);
                        
                        using (var stream = new FileStream(firstFilePath, FileMode.Create))
                        {
                            await firstImage.CopyToAsync(stream);
                        }
                        
                        product.ImageUrl = "/images/products/" + firstFileName;

                        // Lưu các ảnh còn lại vào ImageUrls
                        product.ImageUrls = new List<string>();
                        foreach (var file in imageFiles.Skip(1))
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(uploadPath, fileName);
                            
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            
                            product.ImageUrls.Add("/images/products/" + fileName);
                        }
                    }

                    // Kiểm tra CategoryId có tồn tại không
                    var category = await _context.Categories.FindAsync(product.CategoryId);
                    if (category == null)
                    {
                        ModelState.AddModelError("CategoryId", "Danh mục không tồn tại");
                        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                        return View(product);
                    }

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể thêm sản phẩm: " + ex.Message);
                }
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
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
        public async Task<IActionResult> Update(int id, [Bind("Id,Name,Price,Description,CategoryId,ImageUrl")] Product product, 
            List<IFormFile> imageFiles, List<string> imagesToDelete)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = await _context.Products
                        .Include(p => p.Category)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật thông tin cơ bản
                    existingProduct.Name = product.Name;
                    existingProduct.Price = product.Price;
                    existingProduct.Description = product.Description;
                    existingProduct.CategoryId = product.CategoryId;

                    // Xử lý xóa ảnh
                    if (imagesToDelete != null && imagesToDelete.Any())
                    {
                        foreach (var imageUrl in imagesToDelete)
                        {
                            // Xóa file vật lý
                            var imagePath = Path.Combine(
                                Directory.GetCurrentDirectory(), 
                                "wwwroot", 
                                imageUrl.TrimStart('/'));
                            
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }

                            // Cập nhật ImageUrl và ImageUrls
                            if (existingProduct.ImageUrl == imageUrl)
                            {
                                existingProduct.ImageUrl = null;
                            }
                            if (existingProduct.ImageUrls != null)
                            {
                                existingProduct.ImageUrls.Remove(imageUrl);
                            }
                        }
                    }

                    // Xử lý thêm ảnh mới
                    if (imageFiles != null && imageFiles.Any())
                    {
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        foreach (var file in imageFiles)
                        {
                            if (file.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                                var filePath = Path.Combine(uploadPath, fileName);
                                
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var imageUrl = "/images/products/" + fileName;

                                // Nếu chưa có ảnh chính thì set ảnh đầu tiên làm ảnh chính
                                if (string.IsNullOrEmpty(existingProduct.ImageUrl))
                                {
                                    existingProduct.ImageUrl = imageUrl;
                                }
                                else
                                {
                                    if (existingProduct.ImageUrls == null)
                                    {
                                        existingProduct.ImageUrls = new List<string>();
                                    }
                                    existingProduct.ImageUrls.Add(imageUrl);
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi cập nhật: " + ex.Message;
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
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
            await _productRepository.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
