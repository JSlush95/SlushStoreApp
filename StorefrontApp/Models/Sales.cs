using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    public class Sales
    {
        [Key]
        public int SalesID { get; set; }
        [Required]
        public int BuyerID { get; set; }
        [Required]
        public int OrderID { get; set; }
        [Required]
        public int PaymentMethodID { get; set; }
        [Required]
        public float Price { get; set; }
        [Required]
        public string ShippingAddress { get; set; }
        [Required]
        public DateTime PurchaseDate { get; set; }
        [ForeignKey("BuyerID")]
        public virtual StoreAccount StoreAccount { get; set; }
        [ForeignKey("OrderID")]
        public virtual Order order { get; set; }
        [ForeignKey("PaymentMethodID")]
        public PaymentMethod PaymentMethod { get; set; }
    }
}