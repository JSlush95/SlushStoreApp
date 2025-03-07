﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontAppCore.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        [Required]
        public int? BuyerID { get; set; }
        [Required]
        public int PaymentMethodID { get; set; }
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalPrice { get; set; }
        [Required]
        public string ShippingAddress { get; set; }
        [Required]
        public OrderStatus Status { get; set; }
        [Required]
        public DateTime PurchaseDate { get; set; }
        // Utilized when a user deletes a payment method related to an Order, to prevent deletion of the Order.
        // This is done so the original data is preserved, only use this in that scenario.
        public int? DeletedPaymentMethodID { get; set; }     

        [ForeignKey("BuyerID")]
        public virtual StoreAccount StoreAccount { get; set; }
        [ForeignKey("PaymentMethodID")]
        public PaymentMethod PaymentMethod { get; set; }
        [ForeignKey("DeletedPaymentMethodID")]
        public PaymentMethod DeletedPaymentMethod { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Approved,
        Refunded
    }
}