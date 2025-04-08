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
        private const int PAGE_SIZE = 8; // Đặt hằng số cho số sản phẩm mỗi trang

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        public async Task<IActionResult> Index(
            string searchString, 
            int? category,
            string sortOrder = "newest",
            int page = 1)
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

            // Sắp xếp
            query = sortOrder switch
            {
                "price-asc" => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                "name-asc" => query.OrderBy(p => p.Name),
                "name-desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderByDescending(p => p.Id) // "newest" là mặc định
            };
            ViewBag.CurrentSort = sortOrder;

            // Đếm tổng số sản phẩm để phân trang
            var totalItems = await query.CountAsync();
            
            // Phân trang
            var products = await query
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToListAsync();

            // Tính toán thông tin phân trang
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);
            ViewBag.TotalItems = totalItems;
            ViewBag.StartItem = Math.Min((page - 1) * PAGE_SIZE + 1, totalItems);
            ViewBag.EndItem = Math.Min(page * PAGE_SIZE, totalItems);

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
