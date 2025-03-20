using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebsiteBanHang.Interfaces;
using WebsiteBanHang.Models;
using WebsiteBanHang.Repositories;

namespace WebsiteBanHang.Areas.Admin.Controllers
{
 [Area("Admin")]
 [Authorize(Roles = SD.Role_Admin)]
 public class ProductController : Controller
 {
     private readonly IProductRepository _productRepository;
     private readonly ICategoryRepository _categoryRepository;
     public IActionResult Index()
     {

         return View();
     }
 }
}
