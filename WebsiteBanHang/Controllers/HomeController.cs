using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebsiteBanHang.Models;
using WebsiteBanHang.Repositories;
using Microsoft.EntityFrameworkCore;
using WebsiteBanHang.Data;
using System.Threading.Tasks;
using System.Security.Claims;
using WebsiteBanHang.Enums;

namespace WebsiteBanHang.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, IProductRepository productRepository, ApplicationDbContext context)
        {
            _logger = logger;
            _productRepository = productRepository;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Nếu là admin, chuyển hướng qua trang admin
            if (User.IsInRole(SD.Role_Admin))
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            
            // Lấy danh sách sản phẩm mới cho user
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            
            // Đếm số lượng giỏ hàng
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Đếm số lượng sản phẩm trong giỏ hàng
                ViewBag.CartCount = await _context.OrderDetails
                    .Where(od => od.Order.UserId == userId && od.Order.Status == (int)OrderStatus.Pending)
                    .SumAsync(od => od.Quantity);
            }
            
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
