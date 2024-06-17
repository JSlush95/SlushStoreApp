using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        [Display(Name = "Types")]
        public List<CheckBoxItem> ProductTypeOptions { get; set; }
        [Display(Name = "Suppliers")]
        public List<CheckBoxItem> SuppliersList { get; set; }
        [Display(Name = "Search by Name")]
        public string SearchInput { get; set; }
        [Required]
        public string SortStyleSelect { get; set; }
        [Display(Name = "Sort Options")]
        public Sort SortOptions { get; set; }
        [Required]
        [Display(Name = "Quantity")]
        [Range(0, Int32.MaxValue, ErrorMessage = "Please enter a valid quantity.")]
        public int Quantity { get; set; }
        public bool LoggedIn { get; set; }
    }

    public class CheckBoxItem
    {
        public string NameOrType { get; set; }
        public bool Checked { get; set; }
    }

    public enum Sort
    {
        [Display(Name = "Ascending by Name")]
        AscendingByName,
        [Display(Name = "Descending by Name")]
        DescendingByName,
        [Display(Name = "Ascending by Price")]
        AscendingByPrice,
        [Display(Name = "Descending by Price")]
        DescendingByPrice,
        [Display(Name = "Ascending by Supplier")]
        AscendingBySupplier,
        [Display(Name = "Descending by Supplier")]
        DescendingBySupplier
    }
}