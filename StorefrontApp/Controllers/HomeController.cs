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
        // Necessary extension utility, as the generic type can be declared explicitly instead of type inference, which can be faulty at times.
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

        private int GetCurrentUserId()
        {
            return User.Identity.GetUserId<int>();
        }

        private List<CheckBoxItem> GetProductTypes(IQueryable<Product> productsSet, List<string> selectedTypes)
        {
            var types = productsSet.Select(p => p.ProductType).Distinct().ToList();
            var productTypes = types.Select(type => new CheckBoxItem
            {
                NameOrType = type,
                Checked = selectedTypes.Contains(type)
            }).ToList();

            return productTypes;
        }

        private List<CheckBoxItem> GetSuppliers(IQueryable<Supplier> suppliersSet, List<string> selectedSuppliers)
        {
            var suppliers = suppliersSet.Select(s => s.SupplierName).Distinct().ToList();
            var suppliersList =  suppliers.Select(supplier => new CheckBoxItem
            {
                NameOrType = supplier,
                Checked = selectedSuppliers.Contains(supplier)
            }).ToList();

            return suppliersList;
        }

        public ActionResult QueryStringDelegate(HomeViewModel model)
        {
            // This set of null-coalesce operator statements are from the add/remove from cart routes.
            model.ProductTypeOptions = model.ProductTypeOptions ?? new List<CheckBoxItem>();
            model.SuppliersList = model.SuppliersList ?? new List<CheckBoxItem>();

            // Preparing the selected types and suppliers for the delegation.
            // Checking if the SelectedProductTypes is null or not determines if this was done from add/remove from cart. The data already exists and is passed in, no need to parse the options.
            var selectedTypes = (model.SelectedProductTypes != null) ? model.SelectedProductTypes.Split(',').ToList() : model.ProductTypeOptions
                .Where(pt => pt.Checked)
                .Select(pt => pt.NameOrType)
                .ToList();
            var selectedSuppliers = (model.SelectedSuppliers != null) ? model.SelectedSuppliers.Split(',').ToList() : model.SuppliersList
                .Where(sl => sl.Checked)
                .Select(sl => sl.NameOrType)
                .ToList();

            // Redirecting to Index with the necessary parameters.
            return RedirectToAction("Index", new
            {
                page = model.CurrentPage,
                searchInput = model.SearchInput,
                sortOptions = model.SortOptions,
                selectedTypes = selectedTypes.Any() ? string.Join(",", selectedTypes) : null,
                selectedSuppliers = selectedSuppliers.Any() ? string.Join(",", selectedSuppliers) : null
            });
        }

        public ActionResult Index(int? page, string searchInput, Sort? sortOptions, string selectedTypes, string selectedSuppliers)
        {
            var userId = GetCurrentUserId();
            var productsSet = _dbContext.Products;
            var suppliersSet = _dbContext.Suppliers;
            sortOptions = sortOptions ?? Sort.AscendingByName;

            // Extracting the possible check marked options.
            List<string> checkedProducts = (!string.IsNullOrEmpty(selectedTypes)) ? selectedTypes.Split(',').ToList() : new List<string>();
            List<string> checkedSuppliers = (!string.IsNullOrEmpty(selectedSuppliers)) ? selectedSuppliers.Split(',').ToList() : new List<string>();

            // Populating the default list for all product types and suppliers. Used for the checkboxes for search queries.
            List<CheckBoxItem> productsTypeList = GetProductTypes(productsSet, checkedProducts);
            List<CheckBoxItem> suppliersList = GetSuppliers(suppliersSet, checkedSuppliers);

            IQueryable<Product> filteredProducts = FilterProducts(productsSet, searchInput, checkedProducts, checkedSuppliers);

            IQueryable<Product> sortedProducts;
            switch (sortOptions)
            {
                case Sort.AscendingByName:
                    sortedProducts = filteredProducts.OrderBy(p => p.ProductName);
                    break;

                case Sort.DescendingByName:
                    sortedProducts = filteredProducts.OrderByDescending(p => p.ProductName);
                    break;

                case Sort.AscendingByPrice:
                    sortedProducts = filteredProducts.OrderBy(p => p.Price);
                    break;

                case Sort.DescendingByPrice:
                    sortedProducts = filteredProducts.OrderByDescending(p => p.Price);
                    break;

                case Sort.AscendingBySupplier:
                    sortedProducts = filteredProducts.OrderBy(p => p.Supplier.SupplierName);
                    break;

                case Sort.DescendingBySupplier:
                    sortedProducts = filteredProducts.OrderByDescending(p => p.Supplier.SupplierName);
                    break;

                default:
                    sortedProducts = filteredProducts.OrderBy(p => p.ProductID);
                    break;
            }

            int pageSize = 8;
            int pageNumber = (page ?? 1);
            var paginatedProducts = sortedProducts.ToPagedList(pageNumber, pageSize);

            bool storeAccountCreated = _dbContext.StoreAccounts.Any(sa => sa.HolderID == userId);

            var viewModel = new HomeViewModel
            {
                Products = paginatedProducts,
                SortOptions = sortOptions.Value,
                SearchInput = searchInput,
                ProductTypeOptions = productsTypeList,
                SuppliersList = suppliersList,
                SelectedProductTypes = selectedTypes,
                SelectedSuppliers = selectedSuppliers,
                StoreAccountCreated = storeAccountCreated,
                LoggedIn = User.Identity.IsAuthenticated,
                CurrentPage = page,
                ShoppingCartItems = (User.Identity.IsAuthenticated) ? _dbContext.ShoppingCarts.Include(sc => sc.ShoppingCartItems).FirstOrDefault(sc => sc.Account.HolderID == userId)?.ShoppingCartItems.ToList() : null
            };

            return View(viewModel);
        }

        public IQueryable<Product> FilterProducts(DbSet<Product> productsList, string searchInput, List<string> checkedProductTypes, List<string> checkedSupplierNames)
        {
           // Filtering the products based on the search inputs and the checked entities, only if they are aren't null.
            var filteredProducts = productsList.Where(p =>
                (string.IsNullOrEmpty(searchInput) || p.ProductName.Contains(searchInput)) &&
                (!checkedProductTypes.Any() || checkedProductTypes.Contains(p.ProductType)) &&
                (!checkedSupplierNames.Any() || checkedSupplierNames.Contains(p.Supplier.SupplierName)));

            return filteredProducts;
        }

        // POST: /Home/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddToCart(int productID, int quantity, string searchInput, string sortOptions, string selectedTypes, string selectedSuppliers, int? page)
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
                .FirstOrDefaultAsync() ?? new ShoppingCart  // Null-coalescing operator for instantiation.
                {
                    AccountID = userId,
                    ShoppingCartItems = new List<ShoppingCartItem>()
                };
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

            // Redirect to the QueryStringDelegate action to preserve query string parameters.
            return RedirectToAction("QueryStringDelegate", new
            {
                CurrentPage = page,
                SearchInput = searchInput,
                SortOptions = sortOptions,
                SelectedProductTypes = selectedTypes,
                SelectedSuppliers = selectedSuppliers
            });
        }

        // POST: /Home/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveFromCart(int productID, int shoppingCartID, string returnControllerPath, string searchInput, string sortOptions, string selectedTypes, string selectedSuppliers, int? page)
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

            // Determining the action based on returnControllerPath. This function is used in the other Manage controller.
            string actionName = returnControllerPath == "Manage" ? nameof(ManageController.Index) : nameof(HomeController.QueryStringDelegate);

            // If returning to ManageController.Index, then we simply direct to its Index controller, as it populates its own data automatically.
            if (returnControllerPath == "Manage")
            {
                return RedirectToAction(actionName, returnControllerPath);
            }
            else // For HomeController.QueryStringDelegate, we include CurrentPage (for pagination) and its current search query data.
            {

                return RedirectToAction(actionName, returnControllerPath, new
                {
                    CurrentPage = page,
                    SearchInput = searchInput,
                    SortOptions = sortOptions,
                    SelectedProductTypes = selectedTypes,
                    SelectedSuppliers = selectedSuppliers
                });
            }
        }

        // POST: /Home/AddToWishlist
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

        // POST: /Home/RemoveFromWishlist
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