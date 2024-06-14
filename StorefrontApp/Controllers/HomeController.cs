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

        public HashSet<string> GetProductTypes(DbSet<Product> products)
        {
            var productsList = new HashSet<string>();

            if (products == null || !products.Any())
            {
                return null;
            }

            foreach (var product in products)
            {
                productsList.Add(product.ProductType.ToLower());
            }

            return productsList;
        }

        private int GetCurrentUserId()
        {
            return User.Identity.GetUserId<int>();
        }

        public ActionResult Index(int? page, string searchQuery, HashSet<string> selectedTypes)
        {
            ViewBag.NameParam = String.IsNullOrEmpty(searchQuery) ? "name_desc" : "";
            ViewBag.PriceParam = searchQuery == "Price" ? "price_desc" : "Price";
            var userId = GetCurrentUserId();
            var products = _dbContext.Products;
            var productsTypeList = GetProductTypes(products);
            var shoppingCart = _dbContext.ShoppingCarts
                .Where(sc => sc.Account.HolderID == userId)
                .ToHashSet();

            IEnumerable<Product> SortedProducts;

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }

            if (selectedTypes != null && selectedTypes.Any())
            {
                products = (DbSet<Product>)products.Where(p => selectedTypes.Contains(p.ProductType));
            }

            switch (searchQuery)
            {
                case "name_desc":
                    SortedProducts = products.OrderByDescending(p => p.ProductName);
                    break;

                case "Price":
                    SortedProducts = products.OrderBy(p => p.Price);
                    break;

                case "price_desc":
                    SortedProducts = products.OrderByDescending(p => p.Price);
                    break;

                default:
                    SortedProducts = products.OrderBy(p => p.ProductName);
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
                LoggedIn = User.Identity.IsAuthenticated
            };

            return View(viewModel);
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
                .Where(sc => sc.Account.HolderID == userId)
                .FirstOrDefaultAsync();

            if (userCart == null)
            {
                var shoppingCart = new ShoppingCart
                {
                    AccountID = userId,
                };
                _dbContext.ShoppingCarts.Add(shoppingCart);

                try
                {
                    await _dbContext.SaveChangesAsync();
                    userCart = shoppingCart;    // Update reference, since the old one was empty/null.
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error creating a new shopping cart: " + ex.Message;
                    return View("CustomError");
                }
            }

            var cartItem = new ShoppingCartItem
            {
                ShoppingCartID = userCart.ShoppingCartID,
                ProductID = productID,
                Quantity = quantity
            };

            userCart.ShoppingCartItems.Add(cartItem);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error adding item to shopping cart: " + ex.Message;
                return View("CustomError");
            }

            return RedirectToAction("Index");
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
                .Where(wl => wl.Account.HolderID == userId)
                .FirstOrDefaultAsync();

            if (userWishlist == null)
            {
                var wishList = new Wishlist
                {
                    AccountID = userId
                };
                _dbContext.Wishlists.Add(wishList);

                try
                {
                    await _dbContext.SaveChangesAsync();
                    userWishlist = wishList;    // Update reference, since the old one was empty/null.
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error creating a new wishlist: " + ex.Message;
                    return View("CustomError");
                }
            }

            var wishlistItem = new WishlistItem
            {
                WishlistID = userWishlist.WishlistID,
                ProductID = productID,
                Quantity = quantity
            };

            userWishlist.WishlistItems.Add(wishlistItem);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error adding item to the wishlist: " + ex.Message;
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