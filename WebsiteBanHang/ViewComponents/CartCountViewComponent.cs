using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebsiteBanHang.Data;
using WebsiteBanHang.Enums;

namespace WebsiteBanHang.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartCountViewComponent(
            ApplicationDbContext context, 
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy ClaimsPrincipal từ HttpContext
            var user = _httpContextAccessor.HttpContext?.User;

            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (user == null || !user.Identity.IsAuthenticated)
                return Content("0");

            // Lấy ID người dùng
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            // Nếu không có userId, trả về 0
            if (string.IsNullOrEmpty(userId))
                return Content("0");

            // Đếm số lượng sản phẩm trong giỏ hàng
            var cartCount = await _context.OrderDetails
                .Where(od => od.Order.UserId == userId && 
                       od.Order.Status == (int)OrderStatus.Pending)
                .SumAsync(od => od.Quantity);

            return Content(cartCount.ToString());
        }
    }
} 