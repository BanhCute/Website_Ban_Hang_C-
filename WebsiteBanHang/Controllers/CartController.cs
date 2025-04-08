using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using WebsiteBanHang.Data;
using WebsiteBanHang.Enums;
using WebsiteBanHang.Models;
using System.Text.Json;
using System.Drawing;
using System.IO;
using QRCoder;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using WebsiteBanHang.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using WebsiteBanHang.Models.ViewModels;

namespace WebsiteBanHang.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<CartController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
            
            // Tạo thư mục nếu chưa tồn tại
            var qrCodePath = Path.Combine(_environment.WebRootPath, "uploads", "qrcodes");
            if (!Directory.Exists(qrCodePath))
            {
                Directory.CreateDirectory(qrCodePath);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            try
            {
                // Kiểm tra đăng nhập
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Kiểm tra sản phẩm tồn tại
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                // Kiểm tra số lượng tồn kho
                if (product.Quantity < quantity)
                {
                    return Json(new { success = false, message = $"Sản phẩm chỉ còn {product.Quantity} sản phẩm" });
                }

                // Tìm hoặc tạo đơn hàng pending
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == (int)OrderStatus.Pending);

                if (order == null)
                {
                    // Tạo đơn hàng mới
                    order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        Status = (int)OrderStatus.Pending,
                        ShippingAddress = "",  // Thêm giá trị mặc định
                        PhoneNumber = "",      // Thêm giá trị mặc định
                        Note = "",             // Thêm giá trị mặc định
                        TotalAmount = 0,       // Sẽ được cập nhật sau
                        OrderDetails = new List<OrderDetail>()
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Lưu order trước để có OrderId
                }

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var orderDetail = order.OrderDetails
                    .FirstOrDefault(od => od.ProductId == productId);

                if (orderDetail != null)
                {
                    // Cập nhật số lượng nếu sản phẩm đã có trong giỏ
                    if (orderDetail.Quantity + quantity > product.Quantity)
                    {
                        return Json(new { success = false, message = $"Số lượng vượt quá tồn kho (còn {product.Quantity} sản phẩm)" });
                    }
                    orderDetail.Quantity += quantity;
                }
                else
                {
                    // Thêm sản phẩm mới vào giỏ
                    orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        Price = product.Price
                    };
                    order.OrderDetails.Add(orderDetail);
                }

                // Cập nhật tổng tiền
                order.TotalAmount = order.OrderDetails.Sum(od => od.Price * od.Quantity);

                await _context.SaveChangesAsync();

                // Lấy tổng số lượng trong giỏ hàng
                var cartCount = order.OrderDetails.Sum(od => od.Quantity);

                return Json(new { 
                    success = true, 
                    cartCount = cartCount,
                    message = "Đã thêm vào giỏ hàng"
                });
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                _logger.LogError($"Error in AddToCart: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào giỏ hàng" });
            }
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Lấy đơn hàng pending và chi tiết đơn hàng
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == (int)OrderStatus.Pending);

            if (order == null)
            {
                return View(new List<OrderDetail>());
            }

            return View(order.OrderDetails.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Content("0");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Đếm tổng số lượng sản phẩm trong giỏ hàng từ database
                var cartCount = await _context.OrderDetails
                    .Where(od => od.Order.UserId == userId && od.Order.Status == (int)OrderStatus.Pending)
                    .SumAsync(od => od.Quantity);

                return Content(cartCount.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCartCount: {ex.Message}");
                return Content("0");
            }
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
                    .Include(o => o.OrderDetails)
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

                // Kiểm tra số lượng tồn kho
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                if (quantity > product.Quantity)
                {
                    return Json(new { success = false, message = $"Chỉ còn {product.Quantity} sản phẩm trong kho" });
                }

                // Cập nhật số lượng
                orderDetail.Quantity = quantity;
                
                // Cập nhật tổng tiền đơn hàng
                order.TotalAmount = order.OrderDetails.Sum(od => od.Price * od.Quantity);
                
                await _context.SaveChangesAsync();

                // Tính tổng tiền của sản phẩm
                decimal itemTotal = orderDetail.Price * orderDetail.Quantity;
                
                // Đếm tổng số lượng sản phẩm trong giỏ hàng
                var cartCount = order.OrderDetails.Sum(od => od.Quantity);

                return Json(new { 
                    success = true, 
                    cartCount = cartCount,
                    itemTotal = itemTotal.ToString("N0"),
                    cartTotal = order.TotalAmount.ToString("N0"),
                    message = "Cập nhật giỏ hàng thành công" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateQuantity: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Tìm order detail cần xóa
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Order)
                    .FirstOrDefaultAsync(od => 
                        od.ProductId == productId && 
                        od.Order.UserId == userId && 
                        od.Order.Status == (int)OrderStatus.Pending);

                if (orderDetail == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                // Xóa order detail
                _context.OrderDetails.Remove(orderDetail);
                await _context.SaveChangesAsync();

                // Kiểm tra nếu không còn sản phẩm nào thì xóa order
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == orderDetail.OrderId);

                if (order != null && !order.OrderDetails.Any())
                {
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Tìm order đang pending
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == (int)OrderStatus.Pending);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng" });
                }

                // Xóa tất cả order details
                _context.OrderDetails.RemoveRange(order.OrderDetails);
                // Xóa order
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
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
                    return RedirectToAction("Index");
                }

                // Lấy thông tin user
                var user = await _userManager.GetUserAsync(User);
                
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
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId && 
                                            o.UserId == userId && 
                                            o.Status == (int)OrderStatus.Pending);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Cập nhật thông tin đơn hàng
                order.ShippingAddress = model.ShippingAddress;
                order.PhoneNumber = model.PhoneNumber;
                order.Note = model.Note;
                order.Status = (int)OrderStatus.Processing;
                order.TotalAmount = order.OrderDetails.Sum(od => od.Price * od.Quantity);

                // Tạo mã giao dịch và mã xác nhận
                string transactionCode = Guid.NewGuid().ToString("N").Substring(0, 12);
                string verificationCode = new Random().Next(100000, 999999).ToString();

                // Tạo QR code với định dạng "Mã Xác Nhận: [mã số]"
                var qrContent = $"Mã Xác Nhận: {verificationCode}";

                string fileName = $"qr_order_{order.Id}.png";
                string qrPath = Path.Combine("uploads", "qrcodes", fileName);
                string fullPath = Path.Combine(_environment.WebRootPath, qrPath);

                // Tạo QR code image
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q))
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    await System.IO.File.WriteAllBytesAsync(fullPath, qrCodeImage);
                }

                // Tạo payment transaction
                var transaction = new PaymentTransaction
                {
                    OrderId = order.Id,
                    TransactionCode = transactionCode,
                    VerificationCode = verificationCode,
                    Amount = order.TotalAmount,
                    CreatedAt = DateTime.Now,
                    IsPaid = false,
                    QRImagePath = $"/{qrPath.Replace('\\', '/')}"
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Cập nhật số lượng sản phẩm trong kho
                foreach (var item in order.OrderDetails)
                {
                    var product = item.Product;
                    if (product != null)
                    {
                        // Giảm số lượng sản phẩm
                        product.Quantity -= item.Quantity;
                        
                        // Đảm bảo số lượng không âm
                        if (product.Quantity < 0)
                        {
                            product.Quantity = 0;
                        }
                        
                        _context.Products.Update(product);
                    }
                }

                await _context.SaveChangesAsync();

                // Xóa giỏ hàng sau khi thanh toán thành công
                HttpContext.Session.Remove("Cart");

                return Json(new
                {
                    success = true,
                    orderId = order.Id,
                    totalAmount = order.TotalAmount.ToString("N0") + "đ",
                    qrImageUrl = transaction.QRImagePath,
                    verificationCode = verificationCode,
                    message = "Đơn hàng đã được tạo thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Checkout POST: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý đơn hàng" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPayment(string verificationCode, int orderId)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Tìm giao dịch thanh toán
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == orderId && t.VerificationCode == verificationCode);

                if (transaction == null)
                {
                    // Nếu không tìm thấy giao dịch hoặc mã xác nhận không đúng
                    // Tự động hủy đơn hàng và hoàn lại số lượng sản phẩm
                    var orderToCancel = await _context.Orders
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                    if (orderToCancel != null && orderToCancel.Status != (int)OrderStatus.Cancelled)
                    {
                        // Hoàn lại số lượng sản phẩm
                        foreach (var item in orderToCancel.OrderDetails)
                        {
                            if (item.Product != null)
                            {
                                item.Product.Quantity += item.Quantity;
                                _context.Products.Update(item.Product);
                            }
                        }

                        // Cập nhật trạng thái đơn hàng thành "Đã hủy"
                        orderToCancel.Status = (int)OrderStatus.Cancelled;
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = false, message = "Mã xác nhận không hợp lệ" });
                }

                if (transaction.IsPaid)
                {
                    return Json(new { success = false, message = "Đơn hàng này đã được thanh toán" });
                }

                // Tìm đơn hàng
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Cập nhật trạng thái thanh toán
                transaction.IsPaid = true;
                transaction.PaidAt = DateTime.Now;

                // Cập nhật trạng thái đơn hàng thành "Đã xác nhận"
                order.Status = (int)OrderStatus.Paid;

                // Lưu thay đổi
                await _context.SaveChangesAsync();

                // Xóa giỏ hàng sau khi thanh toán thành công
                HttpContext.Session.Remove("Cart");

                return Json(new
                {
                    success = true,
                    message = "Thanh toán thành công",
                    redirectUrl = Url.Action("OrderCompleted", "Cart", new { id = orderId })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in VerifyPayment: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xác nhận thanh toán" });
            }
        }

        [Authorize]
        public async Task<IActionResult> OrderCompleted(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> GeneratePaymentQR(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            try
            {
                // Tạo nội dung QR
                var qrContent = new
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var qrContentJson = JsonSerializer.Serialize(qrContent);

                // Tạo QR code
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContentJson, QRCodeGenerator.ECCLevel.Q))
                    {
                        using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                        {
                            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);
                            
                            // Tạo tên file và đường dẫn
                            string fileName = $"qr_order_{orderId}.png";
                            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "qrcodes", fileName);
                            
                            // Đảm bảo thư mục tồn tại
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            
                            // Lưu file
                            await System.IO.File.WriteAllBytesAsync(filePath, qrCodeAsPngByteArr);

                            return Json(new
                            {
                                success = true,
                                qrImageUrl = $"/uploads/qrcodes/{fileName}",
                                amount = order.TotalAmount
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tạo mã QR: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            return Json(new { isPaid = order?.Status == (int)OrderStatus.Paid });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return NotFound();

            order.Status = (int)OrderStatus.Paid;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Thêm phương thức mới để tự động hủy đơn hàng sau một khoảng thời gian
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Chỉ hủy đơn hàng khi đang ở trạng thái Pending hoặc Processing
                if (order.Status != (int)OrderStatus.Pending && order.Status != (int)OrderStatus.Processing)
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái hiện tại" });
                }

                // Hoàn lại số lượng sản phẩm
                foreach (var item in order.OrderDetails)
                {
                    if (item.Product != null)
                    {
                        item.Product.Quantity += item.Quantity;
                        _context.Products.Update(item.Product);
                    }
                }

                // Cập nhật trạng thái đơn hàng
                order.Status = (int)OrderStatus.Cancelled;
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Đã hủy đơn hàng thành công",
                    redirectUrl = Url.Action("Index", "Product")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CancelOrder: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng" });
            }
        }
    }
} 