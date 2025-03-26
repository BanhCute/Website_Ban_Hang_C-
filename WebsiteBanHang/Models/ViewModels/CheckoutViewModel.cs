using System.ComponentModel.DataAnnotations;

public class CheckoutViewModel
{
    public Order Order { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    public string ShippingAddress { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string PhoneNumber { get; set; }
    
    public string Note { get; set; }
} 