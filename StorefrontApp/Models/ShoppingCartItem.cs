using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("ShoppingCartItems")]
    public class ShoppingCartItem
    {
        [Key]
        public int ShoppingCartItemID { get; set; }
        [Required]
        public int ShoppingCartID { get; set; }
        [Required]
        public int ProductID { get; set; }
        [Required]
        public int Quantity { get; set; }

        [ForeignKey("ShoppingCartID")]
        public virtual ShoppingCart ShoppingCart { get; set; }
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}