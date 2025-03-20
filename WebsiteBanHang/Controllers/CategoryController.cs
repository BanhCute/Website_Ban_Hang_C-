using Microsoft.AspNetCore.Mvc;
using WebsiteBanHang.Data;
using WebsiteBanHang.Interfaces;
using WebsiteBanHang.Models;
using WebsiteBanHang.Repositories;

namespace WebsiteBanHang.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        public CategoryController(ICategoryRepository categoryRepository, ApplicationDbContext context)
        {
            _categoryRepository = categoryRepository;
            _context = context;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        // GET: Category/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: Category/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind("Name,Description")] Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                    
                    // Thêm thông báo để debug
                    Console.WriteLine($"Đã thêm danh mục: {category.Name}");
                    
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                // Thêm thông báo để debug
                Console.WriteLine($"Lỗi: {ex.Message}");
                ModelState.AddModelError("", "Không thể thêm danh mục: " + ex.Message);
            }
            
            return View(category);
        }

        // GET: Category/Update/5
        public async Task<IActionResult> Update(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // POST: Category/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryRepository.UpdateAsync(category);
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Lỗi khi cập nhật danh mục: {ex.Message}";
                }
            }
            return View(category);
        }

        // GET: Category/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _categoryRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Xóa danh mục thành công";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa danh mục: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
} 