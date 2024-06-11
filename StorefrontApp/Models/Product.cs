using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }
        [Required]
        public int SupplierID { get; set; }
        [Required]
        public string ProductName { get; set; }
        [Required]
        public string ProductDescription { get; set; }
        [Required]
        public int ProductType { get; set; }
        [Required]
        public int Stock {  get; set; }
        [ForeignKey("SupplierID")]
        public virtual Supplier supplier { get; set; }
    }
}