using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.WebPages.Html;

namespace StorefrontApp.Models
{
    public class HomeViewModel
    {
        public IPagedList<Product> Products { get; set; }
        public HashSet<ShoppingCart> ShoppingCart { get; set; }
        public HashSet<string> ProductTypeOptions { get; set; }
        public string SearchInput { get; set; }
        [Required]
        public string SortStyleSelect { get; set; }
        [Required]
        public Sort SortOptions { get; set; }
        [Required]
        [Display(Name = "Quantity")]
        [Range(0, Int32.MaxValue, ErrorMessage = "Please enter a valid quantity.")]
        public int Quantity { get; set; }
        public bool Checked { get; set; }
        public bool LoggedIn { get; set; }
    }

    public enum Sort
    {
        AscendingByPrice,
        DescendingByPrice,
        AscendingByName,
        DescendingByName,
        AscendingBySupplier,
        DescendingBySupplier
    }
}