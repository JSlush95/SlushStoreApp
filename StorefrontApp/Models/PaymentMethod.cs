using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("PaymentMethods")]
    public class PaymentMethod
    {
        [Key]
        public int PaymentMethodID { get; set; }
        [Required]
        public int AccountID { get; set; }
        [Required]
        [StringLength(11)]
        public string CardNumber { get; set; }
        [Required]
        [StringLength(5)]
        public string KeyPIN { get; set; }

        [ForeignKey("AccountID")]
        public virtual StoreAccount Account { get; set; }
    }
}