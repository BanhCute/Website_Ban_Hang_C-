using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using WebsiteBanHang.Data;
using WebsiteBanHang.Models;

namespace WebsiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Đếm số lượng sản phẩm
            ViewBag.ProductCount = await _context.Products.CountAsync();
            
            // Đếm số lượng danh mục
            ViewBag.CategoryCount = await _context.Categories.CountAsync();
            
            // Đếm số lượng đơn hàng (nếu có bảng Order)
            // ViewBag.OrderCount = await _context.Orders.CountAsync();
            // Tạm thời để 0 nếu chưa có bảng Order
            ViewBag.OrderCount = 0;
            
            // Đếm số lượng người dùng
            ViewBag.UserCount = await _context.Users.CountAsync();

            // Lấy 5 sản phẩm mới nhất
            var latestProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();
            ViewBag.LatestProducts = latestProducts;

            // Lấy thông tin danh mục cho biểu đồ
            var categories = await _context.Categories
                .Select(c => new { c.Name, ProductCount = c.Products.Count })
                .ToListAsync();
            
            ViewBag.CategoryLabels = string.Join(",", categories.Select(c => $"'{c.Name}'"));
            ViewBag.CategoryData = string.Join(",", categories.Select(c => c.ProductCount));

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
} 