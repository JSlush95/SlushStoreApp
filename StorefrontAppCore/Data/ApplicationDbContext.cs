using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StorefrontAppCore.Models;

namespace StorefrontAppCore.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Overrides data annotations that are defined in the database-representative tables:
            modelBuilder.Entity<User>().Property(u => u.Id).HasColumnName("UserID");
            modelBuilder.Entity<User>().ToTable("Users");

            // Enabling multiple properties on the Review entity with lambda function
            modelBuilder.Entity<Review>(entity => 
            {
                // Using Fluent API to apply the composite key relation for Reviews
                entity.HasKey(r => new { r.AccountID, r.ProductID });

                // Implementing the following due to multiple/cascading paths from multi-relational paths
                // Preventing cascading deletes
                entity.HasOne(r => r.Account)
                    .WithMany()
                    .HasForeignKey(r => r.AccountID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Product)
                    .WithMany()
                    .HasForeignKey(r => r.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Behavior for Order -> StoreAccount
            modelBuilder.Entity<Order>()
                .HasOne(o => o.StoreAccount)
                .WithMany()
                .HasForeignKey(o => o.BuyerID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for Order -> PaymentMethod
            modelBuilder.Entity<Order>()
                .HasOne(o => o.PaymentMethod)
                .WithMany()
                .HasForeignKey(o => o.PaymentMethodID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for OrderItem -> Order
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for OrderItem -> Product
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for PaymentMethod -> StoreAccount
            modelBuilder.Entity<PaymentMethod>()
                .HasOne(pm => pm.Account)
                .WithMany()
                .HasForeignKey(pm => pm.AccountID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for ShoppingCart -> StoreAccount
            modelBuilder.Entity<ShoppingCart>()
                .HasOne(sc => sc.Account)
                .WithMany()
                .HasForeignKey(sc => sc.AccountID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for ShoppingCartItem -> Product
            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(sci => sci.Product)
                .WithMany()
                .HasForeignKey(sci => sci.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for ShoppingCartItem -> ShoppingCart
            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(sci => sci.ShoppingCart)
                .WithMany(sc => sc.ShoppingCartItems)   // Stopping foreign key shadowing
                .HasForeignKey(sci => sci.ShoppingCartID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for WishlistItem -> Product
            modelBuilder.Entity<WishlistItem>()
                .HasOne(wli => wli.Product)
                .WithMany()
                .HasForeignKey(wli => wli.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // Behavior for WishlistItem -> Wishlist
            modelBuilder.Entity<WishlistItem>()
                .HasOne(wli => wli.Wishlist)
                .WithMany(wl => wl.WishlistItems)   // Stopping foreign key shadowing
                .HasForeignKey(wli => wli.WishlistID)
                .OnDelete(DeleteBehavior.Restrict);

            // Enabling multiple properties on the User entity with lambda functions
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email)
                .IsUnique();

                entity.Property(e => e.FirstName)
                    .IsRequired(false);

                entity.Property(e => e.LastName)
                    .IsRequired(false);

                entity.Property(e => e.CompanyName)
                    .IsRequired(false);
            });

            modelBuilder.Entity<StoreAccount>()
                .Property(sa => sa.Alias)
                .IsRequired(false);

            // Enabling precision for the price fields
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18, 2)");
        }

        public IQueryable<StoreAccount> GetStoreAccountQuery(int? userID)
        {
            return StoreAccounts
                .Where(sa => sa.HolderID == userID);
        }

        public List<StoreAccount> GetStoreAccountList(int? userID)
        {
            var storeAccountQuery = GetStoreAccountQuery(userID);

            return storeAccountQuery
                .ToList();
        }

        public async Task<List<StoreAccount>> GetStoreAccountListAsync(int? userID)
        {
            var storeAccountQuery = GetStoreAccountQuery(userID);

            return await storeAccountQuery
                .ToListAsync();
        }

        public StoreAccount GetStoreAccount(int? userID)
        {
            var storeAccountQuery = GetStoreAccountQuery(userID);

            return storeAccountQuery
                .Where(sa => sa.HolderID == userID)
                .FirstOrDefault();
        }

        public async Task<StoreAccount> GetStoreAccountAsync(int? userID)
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

        public List<PaymentMethod> GetPaymentMethodList(int? userID)
        {
            return PaymentMethods
                .Where(pm => pm.Account.HolderID == userID)
                .ToList();
        }

        public async Task<List<PaymentMethod>> GetPaymentMethodListAsync(int? userID)
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

        public bool GetExistingPaymentMethod(int? userID, string cardNumber, string keyPIN)
        {
            var existingPaymentMethod = PaymentMethods
                .Where(pm => (pm.CardNumber == cardNumber && pm.KeyPIN == keyPIN) && pm.Account.HolderID == userID)
                .FirstOrDefault();

            return (existingPaymentMethod != null) ? true : false;
        }

        public async Task<PaymentMethod> GetExistingPaymentMethodAsync(int? userID, string cardNumber, string keyPIN)
        {
            return await PaymentMethods
                .Where(pm => (pm.CardNumber == cardNumber && pm.KeyPIN == keyPIN) && pm.Account.HolderID == userID)
                .FirstOrDefaultAsync();
        }

        public List<PaymentMethod> GetActivePaymentMethodsList(int? userID)
        {
            return PaymentMethods
                .Where(pm => pm.Account.HolderID == userID && !pm.Deactivated)
                .ToList();
        }

        public async Task<List<PaymentMethod>> GetActivePaymentMethodsListAsync(int? userID)
        {
            return await PaymentMethods
                .Where(pm => pm.Account.HolderID == userID && !pm.Deactivated)
                .ToListAsync();
        }

        public ShoppingCart GetShoppingCart(int? userID)
        {
            return ShoppingCarts
                .Where(sc => sc.Account.HolderID == userID)
                .Include(sc => sc.Account)
                .Include(sci => sci.ShoppingCartItems)
                .ThenInclude(sci => sci.Product)
                .ThenInclude(p => p.Supplier)
                .ThenInclude(s => s.Account)
                .FirstOrDefault();
        }

        public async Task<ShoppingCart> GetShoppingCartAsync(int? userID)
        {
            return await ShoppingCarts
                .Where(sc => sc.Account.HolderID == userID)
                .Include(sc => sc.Account)
                .Include(sci => sci.ShoppingCartItems)
                .ThenInclude(sci => sci.Product)
                .ThenInclude(p => p.Supplier)
                .ThenInclude(s => s.Account)
                .FirstOrDefaultAsync();
        }

        public List<ShoppingCartItem> GetShoppingCartItemsList(int? userID)
        {
            return ShoppingCartsItems
                .Where(sci => sci.ShoppingCart.Account.HolderID == userID)
                .Include(sci => sci.Product)
                .ThenInclude(p => p.Supplier)
                .ToList();
        }

        public async Task<List<ShoppingCartItem>> GetShoppingCartItemsListAsync(int? userID)
        {
            return await ShoppingCartsItems
                .Where(sci => sci.ShoppingCart.Account.HolderID == userID)
                .Include(sci => sci.Product)
                .ThenInclude(p => p.Supplier)
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

        public Wishlist GetWishlist(int? userID)
        {
            return Wishlists
                .Where(wl => wl.Account.HolderID == userID)
                .FirstOrDefault();
        }

        public async Task<Wishlist> GetWishlistAsync(int? userID)
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
                .Include(o => o.OrderItems)
                .Include(o => o.StoreAccount)
                .FirstOrDefault();
        }

        public async Task<Order> GetOrderAsync(int orderID)
        {
            return await Orders
                .Where(o => o.OrderID == orderID)
                .Include(o => o.OrderItems)
                .Include(o => o.StoreAccount)
                .FirstOrDefaultAsync();
        }

        public List<Order> GetOrdersList(int? userID)
        {
            return Orders
                .Where(o => o.StoreAccount.HolderID == userID)
                .ToList();
        }

        public async Task<List<Order>> GetOrdersListAsync(int? userID)
        {
            return await Orders
                .Where(o => o.StoreAccount.HolderID == userID)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Supplier)
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
    }
}