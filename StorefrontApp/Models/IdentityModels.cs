using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BankingApp.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace StorefrontApp.Models
{
    public class ApplicationDbContext : IdentityDbContext<User, CustomRole, int, CustomUserLogin, CustomUserRole, CustomUserClaim>
    {
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartsItems { get; set; }
        public DbSet<StoreAccount> StoreAccounts { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }

        public ApplicationDbContext()
            : base("DefaultConnection")
        {
        }

        public IQueryable<StoreAccount> GetStoreAccountQuery (int userID)
        {
            return StoreAccounts
                .Where(sa => sa.HolderID == userID);
        }

        public List<StoreAccount> GetStoreAccountList(int userID)
        {
            var storeAccountQuery = GetStoreAccountQuery(userID);

            return storeAccountQuery
                .ToList();
        }

        public async Task<List<StoreAccount>> GetStoreAccountListAsync(int userID)
        {
            var storeAccountQuery = GetStoreAccountQuery(userID);

            return await storeAccountQuery
                .ToListAsync();
        }

        public StoreAccount GetStoreAccount(int userID)
        {
            var storeAccountQuery = GetStoreAccountQuery(userID);

            return storeAccountQuery
                .Where(sa => sa.HolderID == userID)
                .FirstOrDefault();
        }

        public async Task<StoreAccount> GetStoreAccountAsync(int userID)
        {
            var storeAccountQuery = GetStoreAccountQuery(userID);

            return await storeAccountQuery
                .FirstOrDefaultAsync();
        }

        public bool CheckExistingUser(string alias)
        {
            var existingAccount = StoreAccounts
                .Where(sa => sa.Alias == alias)
                .FirstOrDefault();

            return (existingAccount != null) ? true : false;
        }

        public async Task<bool> CheckExistingUserAsync(string alias)
        {
            var existingAccount = await StoreAccounts
                .Where(sa => sa.Alias == alias)
                .FirstOrDefaultAsync();

            return (existingAccount != null) ? true : false;
        }

        public List<PaymentMethod> GetPaymentMethodList(int userID)
        {
            return PaymentMethods
                .Where(pm => pm.Account.HolderID == userID)
                .ToList();
        }

        public async Task<List<PaymentMethod>> GetPaymentMethodListAsync(int userID)
        {
            return await PaymentMethods
                .Where(pm => pm.Account.HolderID == userID)
                .ToListAsync();
        }

        public PaymentMethod GetPaymentMethodFromPKey(int paymentMethodID)
        {
            return PaymentMethods
                .Where(pm => pm.PaymentMethodID == paymentMethodID)
                .FirstOrDefault();
        }

        public async Task<PaymentMethod> GetPaymentMethodFromPKeyAsync(int paymentMethodID)
        {
            return await PaymentMethods
                .Where(pm => pm.PaymentMethodID == paymentMethodID)
                .FirstOrDefaultAsync();
        }

        public bool GetExistingPaymentMethod(int userID, string cardNumber, string keyPIN)
        {
            var existingPaymentMethod = PaymentMethods
                .Where(pm => (pm.CardNumber == cardNumber && pm.KeyPIN == keyPIN) && pm.Account.HolderID == userID)
                .FirstOrDefault();

            return (existingPaymentMethod != null) ? true : false;
        }

        public async Task<PaymentMethod> GetExistingPaymentMethodAsync(int userID, string cardNumber, string keyPIN)
        {
            return await PaymentMethods
                .Where(pm => (pm.CardNumber == cardNumber && pm.KeyPIN == keyPIN) && pm.Account.HolderID == userID)
                .FirstOrDefaultAsync();
        }

        public List<PaymentMethod> GetActivePaymentMethodsList(int UserID)
        {
            return PaymentMethods
                .Where(pm => pm.Account.HolderID == UserID && !pm.IsDeactivated)
                .ToList();
        }

        public async Task<List<PaymentMethod>> GetActivePaymentMethodsListAsync(int UserID)
        {
            return await PaymentMethods
                .Where(pm => pm.Account.HolderID == UserID && !pm.IsDeactivated)
                .ToListAsync();
        }

        public ShoppingCart GetShoppingCart(int UserID)
        {
            return ShoppingCarts
                .Where(sc => sc.Account.HolderID == UserID)
                .FirstOrDefault();
        }

        public async Task<ShoppingCart> GetShoppingCartAsync(int UserID)
        {
            return await ShoppingCarts
                .Where(sc => sc.Account.HolderID == UserID)
                .FirstOrDefaultAsync();
        }

        public List<ShoppingCartItem> GetShoppingCartItemsList(int userID)
        {
            return ShoppingCartsItems
                .Where(sci => sci.ShoppingCart.Account.HolderID == userID)
                .ToList();
        }

        public async Task<List<ShoppingCartItem>> GetShoppingCartItemsListAsync(int userID)
        {
            return await ShoppingCartsItems
                .Where(sci => sci.ShoppingCart.Account.HolderID == userID)
                .ToListAsync();
        }

        public ShoppingCartItem GetShoppingCartItem(int productID, int shoppingCartID)
        {
            return ShoppingCartsItems
                .Where(sci => sci.ShoppingCartID == shoppingCartID && sci.ProductID == productID)
                .FirstOrDefault();
        }

        public async Task<ShoppingCartItem> GetShoppingCartItemAsync(int productID, int shoppingCartID)
        {
            return await ShoppingCartsItems
                .Where(sci => sci.ShoppingCartID == shoppingCartID && sci.ProductID == productID)
                .FirstOrDefaultAsync();
        }

        public Wishlist GetWishlist(int userID)
        {
            return Wishlists
                .Where(wl => wl.Account.HolderID == userID)
                .FirstOrDefault();
        }

        public async Task<Wishlist> GetWishlistAsync(int userID)
        {
            return await Wishlists
                .Where(wl => wl.Account.HolderID == userID)
                .FirstOrDefaultAsync();
        }

        public WishlistItem GetWishlistItem(int productID, int WishlistID)
        {
            return WishlistItems
                .Where(wl => wl.WishlistID == WishlistID && wl.ProductID == productID)
                .FirstOrDefault();
        }

        public async Task<WishlistItem> GetWishlistItemAsync(int productID, int WishlistID)
        {
            return await WishlistItems
                .Where(wl => wl.WishlistID == WishlistID && wl.ProductID == productID)
                .FirstOrDefaultAsync();
        }

        public Order GetOrder(int orderID)
        {
            return Orders
                .Where(o => o.OrderID == orderID)
                .FirstOrDefault();
        }

        public async Task<Order> GetOrderAsync(int orderID)
        {
            return await Orders
                .Where(o => o.OrderID == orderID)
                .FirstOrDefaultAsync();
        }

        public List<Order> GetOrdersList(int userID)
        {
            return Orders
                .Where(o => o.StoreAccount.HolderID == userID)
                .ToList();
        }

        public async Task<List<Order>> GetOrdersListAsync(int userID)
        {
            return await Orders
                .Where(o => o.StoreAccount.HolderID == userID)
                .ToListAsync();
        }

        public List<Order> GetRelatedOrdersList(int paymentMethodID)
        {
            return Orders
                .Where(o => o.PaymentMethod.PaymentMethodID == paymentMethodID)
                .ToList();
        }

        public async Task<List<Order>> GetRelatedOrdersListAsync(int paymentMethodID)
        {
            return await Orders
                .Where(o => o.PaymentMethodID == paymentMethodID)
                .ToListAsync();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customize the table names by using EF's Fluent API. Other configurations can be done here too.
            // Overrides data annotations that are defined in the database-representative tables:
            modelBuilder.Entity<User>().Property(u => u.Id).HasColumnName("UserID");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<CustomRole>().ToTable("Roles");
            modelBuilder.Entity<CustomUserRole>().ToTable("UserRoles");
            modelBuilder.Entity<CustomUserClaim>().ToTable("UserClaims");
            modelBuilder.Entity<CustomUserLogin>().ToTable("UserLogins");

            // Using Fluent API to apply the composite key relation for Reviews.
            modelBuilder.Entity<Review>()
                .HasKey(r => new { r.AccountID, r.ProductID });

            /*
             * Implementing the following due to multiple/cascading paths from multi-relational paths.
             */
            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Account)
                .WithMany()
                .HasForeignKey(r => r.AccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductID)
                .WillCascadeOnDelete(false);
            
            // Behavior for Order -> StoreAccount
            modelBuilder.Entity<Order>()
                .HasRequired(o => o.StoreAccount)
                .WithMany()
                .HasForeignKey(o => o.BuyerID)
                .WillCascadeOnDelete(false);

            // Behavior for Order -> PaymentMethod
            modelBuilder.Entity<Order>()
                .HasRequired(o => o.PaymentMethod)
                .WithMany()
                .HasForeignKey(o => o.PaymentMethodID)
                .WillCascadeOnDelete(false);

            // Behavior for OrderItem -> Order
            modelBuilder.Entity<OrderItem>()
                .HasRequired(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderID)
                .WillCascadeOnDelete(false);

            // Behavior for OrderItem -> Product
            modelBuilder.Entity<OrderItem>()
                .HasRequired(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductID)
                .WillCascadeOnDelete(false);

            // Behavior for PaymentMethod -> StoreAccount
            modelBuilder.Entity<PaymentMethod>()
                .HasRequired(pm => pm.Account)
                .WithMany()
                .HasForeignKey(pm => pm.AccountID)
                .WillCascadeOnDelete(false);

            // Behavior for ShoppingCart -> StoreAccount
            modelBuilder.Entity<ShoppingCart>()
                .HasRequired(sc => sc.Account)
                .WithMany()
                .HasForeignKey(sc => sc.AccountID)
                .WillCascadeOnDelete(false);

            // Behavior for ShoppingCartItem -> Product
            modelBuilder.Entity<ShoppingCartItem>()
                .HasRequired(sci => sci.Product)
                .WithMany()
                .HasForeignKey(sci => sci.ProductID)
                .WillCascadeOnDelete (false);

            // Behavior for ShoppingCartItem -> ShoppingCart
            modelBuilder.Entity<ShoppingCartItem>()
                .HasRequired(sci => sci.ShoppingCart)
                .WithMany()
                .HasForeignKey(sci => sci.ShoppingCartID)
                .WillCascadeOnDelete(false);

            // Behavior for WishlistItem -> Product
            modelBuilder.Entity<WishlistItem>()
                .HasRequired(wli => wli.Product)
                .WithMany()
                .HasForeignKey(wli => wli.ProductID)
                .WillCascadeOnDelete(false);

            // Behavior for WishlistItem -> Wishlist
            modelBuilder.Entity<WishlistItem>()
                .HasRequired(wli => wli.Wishlist)
                .WithMany()
                .HasForeignKey(wli => wli.WishlistID)
                .WillCascadeOnDelete(false);

            // Enabling unique constraint on User's emails
            modelBuilder.Entity<User>()
                .HasIndex(account => account.Email)
                .IsUnique();
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}