using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using WebsiteBanHang.Data;
using WebsiteBanHang.Enums;
using WebsiteBanHang.Models;

namespace WebsiteBanHang.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            Console.WriteLine($"AddToCart called - ProductId: {productId}, Quantity: {quantity}");
            Console.WriteLine($"User Authenticated: {User.Identity.IsAuthenticated}");

            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("User not authenticated");
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"User ID: {userId}");

                // Tìm hoặc tạo đơn hàng chưa thanh toán
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == (int)OrderStatus.Pending);

                if (order == null)
                {
                    order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        Status = (int)OrderStatus.Pending,
                        ShippingAddress = "Chưa cập nhật",
                        PhoneNumber = "Chưa cập nhật",
                        Note = "Đơn hàng mới"
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra sản phẩm có tồn tại không
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // Tìm chi tiết đơn hàng
                var orderDetail = await _context.OrderDetails
                    .FirstOrDefaultAsync(od => od.OrderId == order.Id && od.ProductId == productId);

                if (orderDetail != null)
                {
                    // Nếu sản phẩm đã có trong giỏ hàng, tăng số lượng
                    orderDetail.Quantity += quantity;
                }
                else
                {
                    // Nếu sản phẩm chưa có trong giỏ hàng, thêm mới
                    orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        Price = product.Price
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                await _context.SaveChangesAsync();

                // Đếm tổng số lượng sản phẩm trong giỏ hàng
                var cartCount = await _context.OrderDetails
                    .Where(od => od.Order.UserId == userId && od.Order.Status == (int)OrderStatus.Pending)
                    .SumAsync(od => od.Quantity);

                return Json(new { success = true, cartCount = cartCount });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (nếu có)
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 3)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Lấy thông tin giỏ hàng
            var cartItems = await _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Order.UserId == userId && od.Order.Status == (int)OrderStatus.Pending)
                .ToListAsync();

            var totalItems = cartItems.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagedItems = cartItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { count = 0 });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartCount = await _context.OrderDetails
                .Where(od => od.Order.UserId == userId &&
                       od.Order.Status == (int)OrderStatus.Pending)
                .SumAsync(od => od.Quantity);

            return Json(new { count = cartCount });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            try
            {
                // Kiểm tra đăng nhập
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Tìm đơn hàng chưa thanh toán
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == (int)OrderStatus.Pending);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Tìm chi tiết đơn hàng
                var orderDetail = await _context.OrderDetails
                    .FirstOrDefaultAsync(od => od.OrderId == order.Id && od.ProductId == productId);

                if (orderDetail == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                // Cập nhật số lượng
                orderDetail.Quantity = quantity;
                await _context.SaveChangesAsync();

                // Đếm tổng số lượng sản phẩm trong giỏ hàng
                var cartCount = await _context.OrderDetails
                    .Where(od => od.Order.UserId == userId && od.Order.Status == (int)OrderStatus.Pending)
                    .SumAsync(od => od.Quantity);

                return Json(new { success = true, cartCount = cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để xóa sản phẩm" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.ProductId == productId &&
                                         od.Order.UserId == userId &&
                                         od.Order.Status == (int)OrderStatus.Pending);

            if (orderDetail == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }

            _context.OrderDetails.Remove(orderDetail);
            await _context.SaveChangesAsync();

            // Tính lại tổng số lượng trong giỏ hàng
            var cartCount = await _context.OrderDetails
                .Where(od => od.Order.UserId == userId && od.Order.Status == (int)OrderStatus.Pending)
                .SumAsync(od => od.Quantity);

            // Kiểm tra xem giỏ hàng có trống không
            var hasItems = await _context.OrderDetails
                .AnyAsync(od => od.Order.UserId == userId && od.Order.Status == (int)OrderStatus.Pending);

            return Json(new
            {
                success = true,
                cartCount = cartCount,
                isEmpty = !hasItems
            });
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            // Lấy thông tin giỏ hàng hiện tại
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == (int)OrderStatus.Pending);

            if (order == null || order.OrderDetails == null || !order.OrderDetails.Any())
            {
                // Nếu không có đơn hàng hoặc không có sản phẩm trong giỏ hàng
                return RedirectToAction("Index", "Cart");
            }

            // Tính tổng tiền
            order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);

            var checkoutViewModel = new CheckoutViewModel
            {
                Order = order,
                ShippingAddress = user.Address ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            };

            return View(checkoutViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            // Log chi tiết từng bước
            Console.WriteLine("Bắt đầu quá trình Checkout");
            Console.WriteLine($"Địa chỉ giao hàng: {model.ShippingAddress}");
            Console.WriteLine($"Số điện thoại: {model.PhoneNumber}");
            Console.WriteLine($"Ghi chú: {model.Note}");

            // Kiểm tra đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("Người dùng chưa đăng nhập");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"User ID: {userId}");

            // Tìm đơn hàng chưa thanh toán
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == (int)OrderStatus.Pending);

            if (order == null)
            {
                Console.WriteLine("Không tìm thấy đơn hàng chưa thanh toán");
                return RedirectToAction("Index", "Cart");
            }

            Console.WriteLine($"Tìm thấy đơn hàng. Số lượng sản phẩm: {order.OrderDetails.Count}");

            try 
            {
                // Cập nhật thông tin đơn hàng
                order.ShippingAddress = model.ShippingAddress;
                order.PhoneNumber = model.PhoneNumber;
                order.Note = model.Note;
                order.OrderDate = DateTime.Now;
                order.Status = (int)OrderStatus.Confirmed;

                // Tính tổng tiền
                order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);

                Console.WriteLine($"Tổng tiền: {order.TotalAmount}");

                await _context.SaveChangesAsync();

                Console.WriteLine($"Lưu đơn hàng thành công. Mã đơn hàng: {order.Id}");

                // Chuyển hướng đến trang xác nhận đơn hàng
                return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết
                Console.WriteLine($"Lỗi trong quá trình Checkout: {ex.Message}");
                Console.WriteLine($"Chi tiết lỗi: {ex.StackTrace}");
                
                // Thêm thông báo lỗi vào ModelState
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt hàng: " + ex.Message);
                return View(model);
            }
        }

        // Thêm action OrderConfirmation
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            // Kiểm tra xác thực người dùng
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy ID người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Tìm đơn hàng
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            // Kiểm tra đơn hàng có tồn tại không
            if (order == null)
            {
                return NotFound();
            }

            // Chuyển trạng thái đơn hàng sang Confirmed
            order.Status = (int)OrderStatus.Confirmed;
            await _context.SaveChangesAsync();

            // Trả về view OrderCompleted
            return View("OrderCompleted", order);
        }
    }
}