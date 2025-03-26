public class PaymentTransaction
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public string TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsPaid { get; set; } = false;
    public string QRImagePath { get; set; }
} 