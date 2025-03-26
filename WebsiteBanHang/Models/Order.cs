using WebsiteBanHang.Models;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public DateTime OrderDate { get; set; }
    public int Status { get; set; } = 1; // Mặc định là Pending
    public string StatusString
    {
        get
        {
            return Status switch
            {
                1 => "Pending",
                2 => "Processing",
                3 => "Shipped",
                4 => "Completed",
                5 => "Cancelled",
                _ => "Unknown"
            };
        }
    }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; }
    public string PhoneNumber { get; set; }
    public string Note { get; set; }
    public ICollection<OrderDetail> OrderDetails { get; set; }
}