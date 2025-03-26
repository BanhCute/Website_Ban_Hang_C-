namespace WebsiteBanHang.Enums
{
    public enum OrderStatus
    {
        Pending = 1,     // Đơn hàng đang chờ xử lý
        Confirmed = 2,   // Đơn hàng đã được xác nhận
        Shipped = 3,     // Đơn hàng đã được giao
        Delivered = 4,   // Đơn hàng đã được giao thành công
        Cancelled = 5    // Đơn hàng đã bị hủy
    }
} 