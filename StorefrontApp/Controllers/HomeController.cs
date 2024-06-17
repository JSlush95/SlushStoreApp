using Microsoft.AspNet.Identity.Owin;
using StorefrontApp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Net;
using System.Data.Entity.Migrations;
using System.Web.WebPages;

namespace StorefrontApp.Controllers
{
    public static class Extensions
    {
        // Necessary extension utility, as the generic type can be declared explicitly to avoid weird cases by being at mercy via type inference. 
        public static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> source,
            IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }
    }

    public class HomeController : Controller
    {
        private ApplicationDbContext _dbContext;
        private ApplicationUserManager _userManager;

        public HomeController()
            : this(new ApplicationDbContext(), null)
        {
        }

        public HomeController(ApplicationDbContext dbContext, ApplicationUserManager userManager)
        {
            ContextDbManager = dbContext;
            UserManager = userManager;
        }

        public ApplicationDbContext ContextDbManager
        {
            get
            {
                return _dbContext ?? HttpContext.GetOwinContext().Get<ApplicationDbContext>();
            }
            private set
            {
                _dbContext = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public List<CheckBoxItem> GetProductTypes(DbSet<Product> products)
        {
            var productsList = new List<CheckBoxItem>();

            if (products == null || !products.Any())
            {
                return null;
            }

            foreach (var product in products)
            {
                if (!productsList.Any(x => x.NameOrType.Equals(product.ProductType, StringComparison.OrdinalIgnoreCase)))
                {
                    CheckBoxItem item = new CheckBoxItem()
                    {
                        NameOrType = product.ProductType,
                        Checked = false
                    };

                    productsList.Add(item);
                }
            }

            return productsList;
        }

        public List<CheckBoxItem> GetSuppliers(DbSet<Supplier> suppliers)
        {
            var suppliersList = new List<CheckBoxItem>();

            if (suppliers == null || !suppliers.Any())
            {
                return null;
            }

            foreach (var supplier in suppliers)
            {
                if (!suppliersList.Any(x => x.NameOrType.Equals(supplier.SupplierName, StringComparison.OrdinalIgnoreCase)))
                {
                    CheckBoxItem item = new CheckBoxItem()
                    {
                        NameOrType = supplier.SupplierName,
                        Checked = false
                    };

                    suppliersList.Add(item);
                }
            }

            return suppliersList;
        }

        private int GetCurrentUserId()
        {
            return User.Identity.GetUserId<int>();
        }

        public ActionResult Index(int? page, HomeViewModel model)
        {
            var userId = GetCurrentUserId();
            var productsSet = _dbContext.Products;
            var suppliersSet = _dbContext.Suppliers;
            List<CheckBoxItem> productsTypeList = model.ProductTypeOptions;
            List<CheckBoxItem> suppliersList = model.SuppliersList;
            Product[] filteredProducts;

            /*
             * Populating the ProductTypeOptions and SuppliersList, if they are not already populated.
             */ 
            if (productsTypeList == null || !productsTypeList.Any())
            {
                productsTypeList = GetProductTypes(productsSet);
            }
            if (suppliersList == null || !suppliersList.Any())
            {
                suppliersList = GetSuppliers(suppliersSet);
            }

            if (!string.IsNullOrEmpty(model.SearchInput) || productsTypeList.Any(x => x.Checked) || suppliersList.Any(x => x.Checked))
            {
                filteredProducts = FilterProducts(productsSet, model.SearchInput, productsTypeList, suppliersList).ToArray();
            }
            else
            {
                filteredProducts = productsSet.ToArray();
            }
            
            var shoppingCart = _dbContext.ShoppingCarts
                .Include(sc => sc.ShoppingCartItems) // Eagerly loading for the navigation propety of the collection of ShoppingCartItems.
                .Where(sc => sc.Account.HolderID == userId)
                .ToHashSet();

            IEnumerable<Product> SortedProducts;

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"];
            }

            switch (model.SortOptions)
            {
                case Sort.AscendingByName:
                    SortedProducts = filteredProducts.OrderBy(p => p.ProductName);
                    break;

                case Sort.DescendingByName:
                    SortedProducts = filteredProducts.OrderByDescending(p => p.ProductName);
                    break;

                case Sort.AscendingByPrice:
                    SortedProducts = filteredProducts.OrderBy(p => p.Price);
                    break;

                case Sort.DescendingByPrice:
                    SortedProducts = filteredProducts.OrderByDescending(p => p.Price);
                    break;

                case Sort.AscendingBySupplier:
                    SortedProducts = filteredProducts.OrderBy(p => p.Supplier.SupplierName);
                    break;

                case Sort.DescendingBySupplier:
                    SortedProducts = filteredProducts.OrderByDescending(p => p.Supplier.SupplierName);
                    break;

                default:
                    SortedProducts = filteredProducts.OrderBy(p => p.ProductID);
                    break;
            }

            int pageSize = 8;
            int pageNumber = (page ?? 1);
            var paginatedProducts = SortedProducts.ToPagedList(pageNumber, pageSize);

            var viewModel = new HomeViewModel
            {
                Products = paginatedProducts,
                ShoppingCart = shoppingCart,
                ProductTypeOptions = productsTypeList,
                SuppliersList = suppliersList,
                LoggedIn = User.Identity.IsAuthenticated,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IQueryable<Product> FilterProducts(DbSet<Product> productsList, string searchInput, List<CheckBoxItem> productTypesChecked, List<CheckBoxItem> suppliersNamesChecked)
        {
           // Filtering by product types.
            var checkedProductTypes = productTypesChecked
                .Where(ptc => ptc.Checked)
                .Select(ptc => ptc.NameOrType);
            
            // Filtering by supplier names.
            var checkedSupplierNames = suppliersNamesChecked
                .Where(snc => snc.Checked)
                .Select(snc => snc.NameOrType);

            // Filtering the products based on the search inputs and the names of the checkmarked objects only if they are aren't null.
            var filteredProducts = productsList.Where(p =>
                (string.IsNullOrEmpty(searchInput) || p.ProductName.Contains(searchInput)) &&
                (!checkedProductTypes.Any() || checkedProductTypes.Contains(p.ProductType)) &&
                (!checkedSupplierNames.Any() || checkedSupplierNames.Contains(p.Supplier.SupplierName)));

            return filteredProducts;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddToCart(int productID, int quantity)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please choose a non-zero, non-decimal number.";
                return RedirectToAction("Index");
            }

            var userId = GetCurrentUserId();
            var user = await UserManager.FindByIdAsync(userId);
            var userCart = await _dbContext.ShoppingCarts
                .Include(sc => sc.ShoppingCartItems) // Eagerly loading for the navigation propety of the collection of ShoppingCartItems.
                .Where(sc => sc.Account.HolderID == userId)
                .FirstOrDefaultAsync();

            if (userCart == null)
            {
                userCart = new ShoppingCart
                {
                    AccountID = userId,
                    ShoppingCartItems = new List<ShoppingCartItem>()
                };
            }

            var existingCartItem = userCart.ShoppingCartItems
                .FirstOrDefault(item => item.ProductID == productID);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new ShoppingCartItem
                {
                    ShoppingCartID = userCart.ShoppingCartID,
                    ProductID = productID,
                    Quantity = quantity
                };

                userCart.ShoppingCartItems.Add(cartItem);
                _dbContext.ShoppingCarts.AddOrUpdate(userCart);
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error adding the item to the shopping cart.";
                return View("CustomError");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveFromCart(int productID, int shoppingCartID, string returnControllerPath)
        {
            var userCartItem = await _dbContext.ShoppingCartsItems
                .Where(sci => sci.ShoppingCartID == shoppingCartID && sci.ProductID == productID)
                .FirstOrDefaultAsync();

            _dbContext.ShoppingCartsItems.Remove(userCartItem);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error removing the item from the shopping cart.";
                return View("CustomError");
            }

            return RedirectToAction("Index", returnControllerPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddToWishlist(int productID, int quantity)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please choose a non-zero non-decimal number.";
                return RedirectToAction("Index");
            }

            var userId = GetCurrentUserId();
            var user = await UserManager.FindByIdAsync(userId);
            var userWishlist = await _dbContext.Wishlists
                .Include(sc => sc.WishlistItems) // Eagerly loading for the navigation propety of the collection of WishlistItems.
                .Where(wl => wl.Account.HolderID == userId)
                .FirstOrDefaultAsync();

            if (userWishlist == null)
            {
                var wishList = new Wishlist
                {
                    AccountID = userId,
                    WishlistItems = new List<WishlistItem>()
                };
            }

            var existingWishlistItem = userWishlist.WishlistItems
                .FirstOrDefault(item => item.ProductID == productID);

            if (existingWishlistItem != null)
            {
                existingWishlistItem.Quantity += quantity;
            }
            else
            {
                var wishlistItem = new WishlistItem
                {
                    WishlistID = userWishlist.WishlistID,
                    ProductID = productID,
                    Quantity = quantity
                };

                userWishlist.WishlistItems.Add(wishlistItem);
                _dbContext.Wishlists.AddOrUpdate(userWishlist);
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error adding the item to the wishlist.";
                return View("CustomError");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveFromWishlist(int productID, int wishlistID)
        {
            var wishlistItem = await _dbContext.WishlistItems
                .Where(wli => wli.WishlistID == wishlistID && wli.ProductID == productID)
                .FirstOrDefaultAsync();

            _dbContext.WishlistItems.Remove(wishlistItem);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error removing the item from the wishlist.";
                return View("CustomError");
            }

            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            return View();
        }
    }
}