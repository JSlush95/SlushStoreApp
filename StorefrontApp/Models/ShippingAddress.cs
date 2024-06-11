using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("ShippingAddresses")]
    public class ShippingAddress
    {
        [Key]
        public int ShippingAddressID { get; set; }
        [Required]
        public int AccountID { get; set; }
        [Required]
        public string Address { get; set; }
        [ForeignKey("AccountID")]
        public virtual StoreAccount Account { get; set; }
    }
}