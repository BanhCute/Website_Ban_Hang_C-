namespace WebsiteBanHang.Enums
{
    public enum OrderStatus
    {
        Pending = 1,     // Đơn hàng đang chờ xử lý
        Processing = 2,  // Đơn hàng đang xử lý
        Confirmed = 3,   // Đơn hàng đã được xác nhận
        Paid = 4,        // Đơn hàng đã được thanh toán
        Shipped = 5,     // Đơn hàng đã được giao
        Delivered = 6,   // Đơn hàng đã được giao thành công
        Cancelled = 7    // Đơn hàng đã bị hủy
    }
} 