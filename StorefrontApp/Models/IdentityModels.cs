using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using BankingApp.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace StorefrontApp.Models
{
    public class ApplicationDbContext : IdentityDbContext<User, CustomRole, int, CustomUserLogin, CustomUserRole, CustomUserClaim>
    {
        public DbSet<OrderItem> Orders {  get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Order> Sales { get; set; }
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

            // Using Fluent API method for applying the composite key relation for Reviews.
            modelBuilder.Entity<Review>()
                .HasKey(r => new { r.AccountID, r.ProductID });

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

            /*
             * Implementing the following due to multiple/cascading paths from multi-relational links. This would be problematic for Entity Framework otherwise.
             */
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

            /*
             *  Configuring the explicit relationships.
             */
            // One-to-one relationship with StoreAccount
            /*modelBuilder.Entity<Wishlist>()
                .HasRequired(wl => wl.Account)
                .WithMany()
                .HasForeignKey(wl => wl.AccountID);

            modelBuilder.Entity<WishlistItem>()
                .HasRequired(wi => wi.Wishlist)
                .WithMany(wl => wl.WishlistItems)
                .HasForeignKey(wi => wi.WishlistID);

            modelBuilder.Entity<WishlistItem>()
                .HasRequired(wi => wi.Product)
                .WithMany()
                .HasForeignKey(wi => wi.ProductID);

            modelBuilder.Entity<ShoppingCart>()
                .HasRequired(sc => sc.Account)
                .WithOptional(sa => sa.ShoppingCart); // Optional one-to-one relationship, as a StoreAccount may not have a wishlist

            modelBuilder.Entity<ShoppingCartItem>()
                .HasRequired(sci => sci.ShoppingCart)
                .WithMany(sc => sc.ShoppingCartItems)
                .HasForeignKey(sci => sci.ShoppingCartID);

            modelBuilder.Entity<ShoppingCartItem>()
                .HasRequired(sci => sci.Product)
                .WithMany()
                .HasForeignKey(sci => sci.ProductID);*/
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}