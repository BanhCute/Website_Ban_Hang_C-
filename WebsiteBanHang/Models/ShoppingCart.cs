using System.Collections.Generic;
using System.Linq;

public class ShoppingCart
{
    public List<CartItem> Items { get; set; } = new List<CartItem>();

    public void AddItem(CartItem item)
    {
        var existingItem = Items.FirstOrDefault(x => x.ProductId == item.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += item.Quantity;
        }
        else
        {
            Items.Add(item);
        }
    }

    public void RemoveItem(int productId)
    {
        var item = Items.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            Items.Remove(item);
        }
    }

    public decimal TotalAmount => Items.Sum(x => x.Total);
}
