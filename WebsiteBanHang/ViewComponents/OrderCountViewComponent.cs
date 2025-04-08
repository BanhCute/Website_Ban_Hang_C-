using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanHang.Data;
using WebsiteBanHang.Enums;

namespace WebsiteBanHang.ViewComponents
{
    public class OrderCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public OrderCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Đếm số đơn hàng mới (status = 1)
            var newOrderCount = await _context.Orders
                .Where(o => o.Status == (int)OrderStatus.Pending)
                .CountAsync();

            if (newOrderCount > 0)
            {
                return View("Default", newOrderCount);
            }

            return Content(string.Empty);
        }
    }
} 