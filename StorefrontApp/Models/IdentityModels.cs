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
        public DbSet<Order> Orders {  get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Sales> Sales { get; set; }
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<StoreAccount> StoreAccounts { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }

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
            // Composite key configuration for Review
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
             * Implementing the following due to multiple/cascading paths. This would be problematic for Entity Framework otherwise.
             */
            // Disabling cascade delete for Sales -> PaymentMethod.
            modelBuilder.Entity<Sales>()
                .HasRequired(s => s.PaymentMethod)
                .WithMany()
                .HasForeignKey(s => s.PaymentMethodID)
                .WillCascadeOnDelete(false);

            // Disabling cascade delete for Sales -> StoreAccount.
            modelBuilder.Entity<Sales>()
                .HasRequired(s => s.StoreAccount)
                .WithMany()
                .HasForeignKey(s => s.BuyerID)
                .WillCascadeOnDelete(false);

            // Disabling cascade delete for Sales -> Order.
            modelBuilder.Entity<Sales>()
                .HasRequired(s => s.order)
                .WithMany()
                .HasForeignKey(s => s.OrderID)
                .WillCascadeOnDelete(false);

            // Disabling cascade delete for ShoppingCart -> StoreAccount.
            modelBuilder.Entity<ShoppingCart>()
                .HasRequired(s => s.Account)
                .WithMany()
                .HasForeignKey(s => s.AccountID)
                .WillCascadeOnDelete(false);

            // Disabling cascade delete for ShoppingCart -> Product.
            modelBuilder.Entity<ShoppingCart>()
                .HasRequired(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductID)
                .WillCascadeOnDelete(false);

            // Disabling cascade delete for Wishlist -> StoreAccount.
            modelBuilder.Entity<Wishlist>()
                .HasRequired(w => w.Account)
                .WithMany()
                .HasForeignKey(w => w.AccountID)
                .WillCascadeOnDelete(false);

            // Disabling cascade delete for Wishlist -> Product.
            modelBuilder.Entity<Wishlist>()
                .HasRequired(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductID)
                .WillCascadeOnDelete(false);
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}