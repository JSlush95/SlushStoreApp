using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        [Required]
        public int BuyerID { get; set; }
        [Required]
        public int PaymentMethodID { get; set; }
        [Required]
        public float TotalPrice { get; set; }
        [Required]
        public string ShippingAddress { get; set; }
        [Required]
        public OrderStatus Status { get; set; }
        [Required]
        public DateTime PurchaseDate { get; set; }

        [ForeignKey("BuyerID")]
        public virtual StoreAccount StoreAccount { get; set; }
        [ForeignKey("PaymentMethodID")]
        public PaymentMethod PaymentMethod { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Approved,
        Refunded
    }
}