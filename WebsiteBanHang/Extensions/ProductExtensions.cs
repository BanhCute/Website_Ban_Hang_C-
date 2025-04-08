using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebsiteBanHang.Data;
using WebsiteBanHang.Models;

public static class ProductExtensions
{
    public static string GetCategoryName(this Product product, ApplicationDbContext context)
    {
        if (product?.CategoryId == null) return "Không có danh mục";
        
        var category = context.Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        return category?.Name ?? "Không có danh mục";
    }
} 