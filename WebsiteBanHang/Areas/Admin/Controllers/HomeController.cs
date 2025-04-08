using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using WebsiteBanHang.Data;
using WebsiteBanHang.Models;
using Microsoft.AspNetCore.Identity;
using WebsiteBanHang.Enums;
using WebsiteBanHang.Models.ViewModels;

namespace WebsiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalUsers = await _userManager.Users.CountAsync(),
                
                RecentProducts = await _context.Products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.Id)
                    .Take(5)
                    .ToListAsync(),
                    
                CategoryStats = await _context.Categories
                    .Select(c => new CategoryStat 
                    { 
                        CategoryName = c.Name,
                        ProductCount = c.Products.Count 
                    })
                    .ToListAsync(),
                    
                // Thêm dữ liệu mới
                PendingOrders = await _context.Orders
                    .CountAsync(o => o.Status == (int)OrderStatus.Pending),
                ConfirmedOrders = await _context.Orders
                    .CountAsync(o => o.Status == (int)OrderStatus.Paid),
                CancelledOrders = await _context.Orders
                    .CountAsync(o => o.Status == (int)OrderStatus.Cancelled),
                    
                RevenueData = await _context.Orders
                    .Where(o => o.Status == (int)OrderStatus.Paid)
                    .Where(o => o.OrderDate >= DateTime.Now.AddDays(-7))
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new RevenueData
                    {
                        Date = g.Key,
                        Amount = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync(),
                    
                TopProducts = await _context.OrderDetails
                    .Where(od => od.Order.Status == (int)OrderStatus.Paid)
                    .GroupBy(od => od.Product)
                    .Select(g => new TopProduct
                    {
                        Name = g.Key.Name,
                        SoldQuantity = g.Sum(od => od.Quantity)
                    })
                    .OrderByDescending(x => x.SoldQuantity)
                    .Take(5)
                    .ToListAsync()
            };
            
            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
} 