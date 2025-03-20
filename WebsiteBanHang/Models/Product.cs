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
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }
        
        public List<string>? ImageUrls { get; set; }

        public virtual Category? Category { get; set; }

        public ICollection<ProductImage> ProductImages { get; set; }
    }
}
