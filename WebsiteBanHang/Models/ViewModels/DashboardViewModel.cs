using WebsiteBanHang.Models;

public class DashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public List<Product> RecentProducts { get; set; }
    public List<CategoryStat> CategoryStats { get; set; }
    public int PendingOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public List<RevenueData> RevenueData { get; set; }
    public List<TopProduct> TopProducts { get; set; }
}

public class CategoryStat
{
    public string CategoryName { get; set; }
    public int ProductCount { get; set; }
}

public class RevenueData
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class TopProduct
{
    public string Name { get; set; }
    public int SoldQuantity { get; set; }
} 