using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebsiteBanHang.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public CheckoutViewModel()
        {
            // Khởi tạo OrderDetails trong constructor
            OrderDetails = new List<OrderDetail>();
        }

        public int OrderId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "Ghi chú")]
        public string Note { get; set; }
        
        public decimal TotalAmount { get; set; }
        
        public List<OrderDetail> OrderDetails { get; set; }
    }
} 