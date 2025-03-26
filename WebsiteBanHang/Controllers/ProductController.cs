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
        public async Task<IActionResult> Index(int page = 1, int pageSize = 3)
        {
            // Lấy tổng số sản phẩm
            var totalItems = await _context.Products.CountAsync(); // Đếm tổng số sản phẩm

            // Lấy danh sách sản phẩm với phân trang
            var products = await _context.Products
                .OrderBy(p => p.Id) // Sắp xếp theo Id
                .Skip((page - 1) * pageSize) // Bỏ qua số sản phẩm đã hiển thị
                .Take(pageSize) // Lấy số sản phẩm theo pageSize
                .ToListAsync(); // Chờ để lấy danh sách sản phẩm

            ViewBag.CurrentPage = page; // Lưu trang hiện tại
            ViewBag.PageSize = pageSize; // Lưu kích thước trang
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize); // Tính tổng số trang

            return View(products); // Trả về danh sách sản phẩm cho view
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
