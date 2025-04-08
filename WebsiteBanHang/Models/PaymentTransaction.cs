using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanHang.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        public string? TransactionCode { get; set; }

        [StringLength(6)]
        public string? VerificationCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; }
        
        public DateTime? PaidAt { get; set; }

        public bool IsPaid { get; set; } = false;

        public string? QRImagePath { get; set; }
    }
} 