using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using StorefrontAppCore.Utilities;
using StorefrontAppCore.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StorefrontAppCore.Data;
using X.PagedList;
using X.PagedList.Extensions;
using Microsoft.EntityFrameworkCore.Query;

namespace StorefrontAppCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext dbContext, ILogger<HomeController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            string userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int retVal))
            {
                return retVal;
            }

            return null;
        }

        private async Task<User> GetCurrentUserAsync(int? id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
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

        public IActionResult QueryStringDelegate(HomeViewModel model)
        {
            // This set of null-coalesce operator statements are from the add/remove from cart routes
            model.ProductTypeOptions = model.ProductTypeOptions ?? new List<CheckBoxItem>();
            model.SuppliersList = model.SuppliersList ?? new List<CheckBoxItem>();

            // Preparing the selected types and suppliers for the delegation
            // Checking if the SelectedProductTypes is null or not determines if this was done from add/remove from cart. The data already exists and is passed in, no need to parse the options
            var selectedTypes = (model.SelectedProductTypes != null) ? model.SelectedProductTypes.Split(',').ToList() : model.ProductTypeOptions
                .Where(pt => pt.Checked)
                .Select(pt => pt.NameOrType)
                .ToList();
            var selectedSuppliers = (model.SelectedSuppliers != null) ? model.SelectedSuppliers.Split(',').ToList() : model.SuppliersList
                .Where(sl => sl.Checked)
                .Select(sl => sl.NameOrType)
                .ToList();

            // The functionality for page remembering must adjust to stay within the possible pages (the page counts can change on new filters, since last request)
            // Reset to first page if we overflow beyond the boundaries of max pages
            model.CurrentPage = (model.CurrentPage > model.MaxPages)? model.CurrentPage = 1 : model.CurrentPage;

            // Redirecting to Index with the necessary parameters.
            return RedirectToAction(nameof(HomeController.Index), new
            {
                page = model.CurrentPage,
                maxPages = model.MaxPages,
                searchInput = model.SearchInput,
                sortOptions = model.SortOptions,
                selectedTypes = (selectedTypes.Count != 0) ? string.Join(",", selectedTypes) : null,
                selectedSuppliers = (selectedSuppliers.Count != 0) ? string.Join(",", selectedSuppliers) : null
            });
        }

        public IActionResult Index(int? page, string searchInput, Sort? sortOptions, string selectedTypes, string selectedSuppliers)
        {
            var userId = GetCurrentUserId();
            var productsSet = _dbContext.Products
                .Include(p => p.Supplier)   // Eagerly load the required data to prevent nulls
                .ThenInclude(s => s.Account);
            var suppliersSet = _dbContext.Suppliers;
            sortOptions = sortOptions ?? Sort.AscendingByName;

            // Extracting the possible check marked options
            List<string> checkedProducts = (!string.IsNullOrEmpty(selectedTypes)) ? selectedTypes.Split(',').ToList() : new List<string>();
            List<string> checkedSuppliers = (!string.IsNullOrEmpty(selectedSuppliers)) ? selectedSuppliers.Split(',').ToList() : new List<string>();

            // Populating the default list for all product types and suppliers. Used for the checkboxes for search queries
            List<CheckBoxItem> productsTypeList = GetProductTypes(productsSet, checkedProducts);
            List<CheckBoxItem> suppliersList = GetSuppliers(suppliersSet, checkedSuppliers);

            IQueryable<Product> filteredSortedProducts = FilterSortProducts(productsSet, searchInput, checkedProducts, checkedSuppliers, sortOptions);

            int pageSize = 8;
            int pageNumber = (page ?? 1);

            // Calculate the total number of items, then the maximum pages
            int totalItems = filteredSortedProducts.Count();
            int maxPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Calculate the number of items to skip, get the current items
            int skip = (pageNumber - 1) * pageSize;
            var paginatedProducts = filteredSortedProducts.Skip(skip).Take(pageSize).ToList();

            // Convert the result to IPagedList.
            var paginatedProductList = new StaticPagedList<Product>(paginatedProducts, pageNumber, pageSize, filteredSortedProducts.Count());

            bool storeAccountCreated = _dbContext.StoreAccounts.Any(sa => sa.HolderID == userId);

            var viewModel = new HomeViewModel
            {
                Products = paginatedProductList,
                SortOptions = sortOptions.Value,
                SearchInput = searchInput,
                ProductTypeOptions = productsTypeList,
                SuppliersList = suppliersList,
                SelectedProductTypes = selectedTypes,
                SelectedSuppliers = selectedSuppliers,
                StoreAccountCreated = storeAccountCreated,
                LoggedIn = User.Identity.IsAuthenticated,
                CurrentPage = page,
                MaxPages = maxPages,
                ShoppingCartItems = (User.Identity.IsAuthenticated) ? _dbContext.GetShoppingCartItemsList(userId) : null
            };

            return View(viewModel);
        }

        public IQueryable<Product> FilterSortProducts(IIncludableQueryable<Product, StoreAccount> productsList, string searchInput, List<string> checkedProductTypes, List<string> checkedSupplierNames, Sort? sortOptions)
        {
           // Filtering the products based on the search inputs and the checked entities, only if they are aren't null
            var filteredProducts = productsList.Where(p =>
                (string.IsNullOrEmpty(searchInput) || p.ProductName.Contains(searchInput)) &&
                (!checkedProductTypes.Any() || checkedProductTypes.Contains(p.ProductType)) &&
                (!checkedSupplierNames.Any() || checkedSupplierNames.Contains(p.Supplier.SupplierName)));

            switch (sortOptions)
            {
                case Sort.AscendingByName:
                    filteredProducts = filteredProducts.OrderBy(p => p.ProductName);
                    break;

                case Sort.DescendingByName:
                    filteredProducts = filteredProducts.OrderByDescending(p => p.ProductName);
                    break;

                case Sort.AscendingByPrice:
                    filteredProducts = filteredProducts.OrderBy(p => p.Price);
                    break;

                case Sort.DescendingByPrice:
                    filteredProducts = filteredProducts.OrderByDescending(p => p.Price);
                    break;

                case Sort.AscendingBySupplier:
                    filteredProducts = filteredProducts.OrderBy(p => p.Supplier.SupplierName);
                    break;

                case Sort.DescendingBySupplier:
                    filteredProducts = filteredProducts.OrderByDescending(p => p.Supplier.SupplierName);
                    break;

                default:
                    filteredProducts = filteredProducts.OrderBy(p => p.ProductID);
                    break;
            }

            return filteredProducts;
        }

        // POST: /Home/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productID, int quantity, string searchInput, string sortOptions, string selectedTypes, string selectedSuppliers, int? page, int? maxPages)
        {
            var userId = GetCurrentUserId();
            var user = await GetCurrentUserAsync(userId);

            var userCart = await _dbContext.GetShoppingCartAsync(userId) ?? new ShoppingCart  // Null-coalescing operator for instantiation
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
                _dbContext.ShoppingCarts.Update(userCart);
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

            // Redirect to the QueryStringDelegate action to preserve query string parameters
            return RedirectToAction(nameof(HomeController.QueryStringDelegate), new
            {
                CurrentPage = page,
                MaxPages = maxPages,
                SearchInput = searchInput,
                SortOptions = sortOptions,
                SelectedProductTypes = selectedTypes,
                SelectedSuppliers = selectedSuppliers
            });
        }

        // POST: /Home/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int productID, int shoppingCartID, string returnControllerPath, string searchInput, string sortOptions, string selectedTypes, string selectedSuppliers, int? page, int? maxPages)
        {
            var userCartItem = await _dbContext.GetShoppingCartItemAsync(productID, shoppingCartID);
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

            // Determining the action based on returnControllerPath. This function is used in the other Manage controller
            string actionName = returnControllerPath == "Manage" ? nameof(ManageController.Index) : nameof(HomeController.QueryStringDelegate);

            // If returning to ManageController.Index, then we simply direct to its Index controller, as it populates its own data automatically
            if (returnControllerPath == "Manage")
            {
                return RedirectToAction(actionName, returnControllerPath);
            }
            else // For HomeController.QueryStringDelegate, we include CurrentPage (for pagination) and its current search query data
            {

                return RedirectToAction(actionName, returnControllerPath, new
                {
                    CurrentPage = page,
                    MaxPages = maxPages,
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
        public async Task<IActionResult> AddToWishlist(int productID, int quantity)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please choose a non-zero non-decimal number.";
                return RedirectToAction(nameof(ManageController.Index));
            }

            var userId = GetCurrentUserId();
            var user = await GetCurrentUserAsync(userId);
            var userWishlist = await _dbContext.GetWishlistAsync(userId);

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
                _dbContext.Wishlists.Update(userWishlist);
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

            return RedirectToAction(nameof(HomeController.Index));
        }

        // POST: /Home/RemoveFromWishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(int productID, int wishlistID)
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

            return RedirectToAction(nameof(HomeController.Index));
        }

        public IActionResult About()
        {
            return View();
        }
    }
}