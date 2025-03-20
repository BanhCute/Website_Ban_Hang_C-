using System.ComponentModel.DataAnnotations;

namespace WebsiteBanHang.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(50, ErrorMessage = "Tên danh mục không được vượt quá 50 ký tự")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string Description { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
