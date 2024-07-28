using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontAppCore.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key, Column(Order = 0)]
        public int AccountID { get; set; }
        [Key, Column(Order = 1)]
        public int ProductID { get; set; }
        [Required]
        public int Rating { get; set; }
        public string CustomerReview {  get; set; }

        [ForeignKey("AccountID")]
        public virtual StoreAccount Account { get; set; }
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}