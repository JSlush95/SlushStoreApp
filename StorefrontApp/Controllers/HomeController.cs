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

        public List<string> GetCheckedItems(List<CheckBoxItem> itemsList)
        {
            var checkedItems = new List<string>();

            foreach (var item in itemsList)
            {
                if (item.Checked)
                {
                    checkedItems.Add(item.NameOrType);
                }
            }

            return checkedItems;
        }

        public List<CheckBoxItem> GetOldChecklist(List<CheckBoxItem> itemsList, List<string> checkedItems)
        {
            List<CheckBoxItem> tempList = new List<CheckBoxItem>();

            foreach (var item in itemsList)
            {
                // Initialize the temporary checklist item with the value that'll always happen, the name.
                CheckBoxItem tempItem = new CheckBoxItem()
                {
                    NameOrType = item.NameOrType
                    // Checked = ??  (Checked value will be decided by existence in checkedItems)
                };

                if (checkedItems.Contains(item.NameOrType))
                {
                    tempItem.Checked = true;
                }
                else
                {
                    tempItem.Checked = false;
                }
                tempList.Add(tempItem);
            }

            return tempList;
        }

        private int GetCurrentUserId()
        {
            return User.Identity.GetUserId<int>();
        }

        public ActionResult Index(int? page, string searchInput, Sort? sortOptions, string selectedTypes, string selectedSuppliers, HomeViewModel model)
        {
            var userId = GetCurrentUserId();
            var productsSet = _dbContext.Products;
            var suppliersSet = _dbContext.Suppliers;
            sortOptions = sortOptions ?? new Sort();
            IQueryable<Product> filteredProducts;
            List<string> checkedProducts, checkedSuppliers;
            var userStoreAccount = _dbContext.StoreAccounts
                .Where(sa => sa.HolderID == userId)
                .FirstOrDefault();

            // Null-coalescing operations. If the model's lists for products/suppliers are null, assign them into a new default list for modification.
            List<CheckBoxItem> productsTypeList = model.ProductTypeOptions ?? GetProductTypes(productsSet);
            List<CheckBoxItem> suppliersList = model.SuppliersList ?? GetSuppliers(suppliersSet);

            // If the query string exists for product types, replace the default list with its checked elements.
            if (selectedTypes != null)
            {
                checkedProducts = selectedTypes.Split('+').ToList();
                // The pagination functionality does not retain model data, therefore, we must manually inject it back in to retain past request state for the UX.
                productsTypeList = GetOldChecklist(productsTypeList, checkedProducts);
            }
            // Otherwise, extract which of the default products are checked.
            else
            {
                checkedProducts = GetCheckedItems(productsTypeList);

                // Creating the query string for the product types, then binding it for the view's pagination routing.
                selectedTypes = string.Join("+", checkedProducts.ToArray());
            }
            // If the query string exists for suppliers, replace the default list with its checked elements.
            if (selectedSuppliers != null)
            {
                checkedSuppliers = selectedSuppliers.Split('+').ToList();
                // The pagination functionality does not retain model data, therefore, we must manually inject it back in to retain past request state for the UX.
                suppliersList = GetOldChecklist(suppliersList, checkedSuppliers);
            }
            // Otherwise, extract which of the default suppliers are checked.
            else
            {
                checkedSuppliers = GetCheckedItems(suppliersList);

                // Creating the query string for the suppliers, then binding it for the view's pagination routing.
                selectedSuppliers = string.Join("+", checkedSuppliers.ToArray());
            }

            // If any filters exist, proceed to use them.
            if (!string.IsNullOrEmpty(searchInput) || checkedProducts.Any() || checkedSuppliers.Any())
            {
                filteredProducts = FilterProducts(productsSet, searchInput, checkedProducts, checkedSuppliers);
            }
            // Otherwise, there exists no filters in place, use the original data.
            else
            {
                filteredProducts = productsSet;
            }

            var shoppingCart = _dbContext.ShoppingCarts
                .Include(sc => sc.ShoppingCartItems) // Eagerly loading for the navigation propety of the collection of ShoppingCartItems.
                .Where(sc => sc.Account.HolderID == userId)
                .FirstOrDefault();

            IQueryable<Product> sortedProducts;

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"];
            }

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

            bool storeAccountCreated = (userStoreAccount != null) ? true : false;

            var viewModel = new HomeViewModel
            {
                Products = paginatedProducts,
                ShoppingCartItems = shoppingCart.ShoppingCartItems.ToList(),
                SortOptions = (Sort)sortOptions,
                SearchInput = searchInput,
                ProductTypeOptions = productsTypeList,
                SuppliersList = suppliersList,
                SelectedProductTypes = selectedTypes,
                SelectedSuppliers = selectedSuppliers,
                LoggedIn = User.Identity.IsAuthenticated,
                StoreAccountCreated = storeAccountCreated
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

        // POST: /Home/RemoveFromCart
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