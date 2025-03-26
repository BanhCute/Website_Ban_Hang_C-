using Microsoft.AspNetCore.Mvc;
using WebsiteBanHang.Interfaces;
using WebsiteBanHang.Models;
using WebsiteBanHang.Repositories;
using PagedList;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebsiteBanHang.Data;

namespace WebsiteBanHang.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        // Chỉ giữ lại các action để xem sản phẩm
        public async Task<IActionResult> Index(
            string searchString, 
            int? category, 
            int page = 1, 
            int pageSize = 2)
        {
            // Lấy danh sách categories để hiển thị dropdown
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;

            // Truy vấn sản phẩm
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => 
                    p.Name.Contains(searchString) || 
                    p.Description.Contains(searchString)
                );
                ViewBag.CurrentFilter = searchString;
            }

            // Lọc theo danh mục
            if (category.HasValue)
            {
                query = query.Where(p => p.CategoryId == category.Value);
                ViewBag.CurrentCategory = category.Value;
            }

            // Phân trang
            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Tính tổng số trang
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

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
    }
}
