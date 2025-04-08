using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebsiteBanHang.Data;
using WebsiteBanHang.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WebsiteBanHang.Extensions;
using WebsiteBanHang.Enums;
using Microsoft.AspNetCore.Http;
using WebsiteBanHang.Models.ViewModels;

namespace WebsiteBanHang.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<OrderController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            
            // Load trực tiếp từ Categories để đảm bảo có dữ liệu
            var categories = await _context.Categories.ToDictionaryAsync(c => c.Id, c => c.Name);
            
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Categories = categories; // Truyền categories vào view

            return View(orders);
        }

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Lấy đơn hàng đang pending của user
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == (int)OrderStatus.Pending);

                if (order == null || !order.OrderDetails.Any())
                {
                    TempData["Error"] = "Không có sản phẩm trong giỏ hàng";
                    return RedirectToAction("Index", "Cart");
                }

                // Lấy thông tin user
                var user = await _userManager.GetUserAsync(User);
                
                // Tạo view model cho checkout
                var checkoutVM = new CheckoutViewModel
                {
                    OrderId = order.Id,
                    TotalAmount = order.OrderDetails.Sum(od => od.Price * od.Quantity),
                    ShippingAddress = user.Address ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Note = "",
                    OrderDetails = order.OrderDetails.ToList()
                };

                return View(checkoutVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Checkout GET: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại sau";
                return RedirectToAction("Index", "Cart");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Lấy đơn hàng đang pending
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId && o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("Index", "Cart");
                }

                // Cập nhật thông tin đơn hàng
                order.ShippingAddress = model.ShippingAddress;
                order.PhoneNumber = model.PhoneNumber;
                order.Note = model.Note;
                order.Status = (int)OrderStatus.Processing;
                order.TotalAmount = model.TotalAmount;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Checkout POST: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi xử lý đơn hàng");
                return View(model);
            }
        }
    }
} 