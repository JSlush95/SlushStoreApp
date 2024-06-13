using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("WishlistItems")]
    public class WishlistItem
    {
        [Key]
        public int WishlistItemID { get; set; }
        [Required]
        public int WishlistID { get; set; }
        [Required]
        public int ProductID { get; set; }
        [Required]
        public int Quantity { get; set; }

        [ForeignKey("WishlistID")]
        public virtual Wishlist Wishlist { get; set; }
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}