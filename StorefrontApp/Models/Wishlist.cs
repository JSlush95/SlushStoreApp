using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("Wishlists")]
    public class Wishlist
    {
        [Key]
        public int WishlistID { get; set; }
        [Required]
        public int AccountID { get; set; }

        [ForeignKey("AccountID")]
        public virtual StoreAccount Account { get; set; }

        public virtual ICollection<WishlistItem> WishlistItems { get; set; }
    }
}