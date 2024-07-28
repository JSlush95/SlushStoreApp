using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontAppCore.Models
{
    [Table("Suppliers")]
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }
        [Required]
        public int AccountID { get; set; }
        [Required]
        public string SupplierName { get; set;}
        [Required]
        public string SupplierDescription { get; set;}
        
        [ForeignKey("AccountID")]
        public virtual StoreAccount Account { get; set; }
    }
}