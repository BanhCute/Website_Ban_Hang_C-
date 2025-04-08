using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanHang.Models
{
    public class Product
    {
        public Product()
        {
            ProductImages = new List<ProductImage>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng sản phẩm")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục sản phẩm")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }
        
        public List<string>? ImageUrls { get; set; }

        public ICollection<ProductImage> ProductImages { get; set; }

        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }

        // Navigation property
        public virtual Category? Category { get; set; }
    }
}
