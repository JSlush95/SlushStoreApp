using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("ShoppingCarts")]
    public class ShoppingCart
    {
        [Key]
        public int ShoppingCartID { get; set; }
        [Required]
        public int AccountID { get; set; }
        [Required]
        public int ProductID { get; set; }
        [Required]
        public int Quantity { get; set; }
        [ForeignKey("AccountID")]
        public virtual StoreAccount Account { get; set; }
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}