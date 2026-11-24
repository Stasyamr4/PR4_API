using Microsoft.EntityFrameworkCore;
using PR4_API.Models;

namespace PR4_API.Data;

public class PR4_APIContext : DbContext
{
    public PR4_APIContext(DbContextOptions<PR4_APIContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(e => { e.ToTable("Address"); e.HasKey(x => x.ID); e.Property(x => x.Name).HasMaxLength(250); });
        modelBuilder.Entity<AddressPVZ>(e => { e.ToTable("AddressPVZ"); e.HasKey(x => x.ID); e.Property(x => x.Name).HasMaxLength(250); });
        modelBuilder.Entity<Basket>(e => { e.ToTable("Basket"); e.HasKey(x => x.ID); e.Property(x => x.Price).HasColumnType("decimal(18,0)"); e.HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<BasketProduct>(e => { e.ToTable("BasketProduct"); e.HasKey(x => x.ID); e.HasOne<Basket>().WithMany().HasForeignKey(x => x.BasketID).OnDelete(DeleteBehavior.Cascade); e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Category>(e => { e.ToTable("Category"); e.HasKey(x => x.ID); e.Property(x => x.Name).HasMaxLength(250); });
        modelBuilder.Entity<Comment>(e => { e.ToTable("Comment"); e.HasKey(x => x.ID); e.Property(x => x.Stars).HasMaxLength(250); e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict); e.HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Like>(e => { e.ToTable("Like"); e.HasKey(x => x.ID); e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict); e.HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Order>(e => { e.ToTable("Order"); e.HasKey(x => x.ID); e.Property(x => x.TotalPrice).HasColumnType("decimal(18,0)"); e.HasOne<User>().WithMany().HasForeignKey(x => x.UserID).OnDelete(DeleteBehavior.Restrict); e.HasOne<Status>().WithMany().HasForeignKey(x => x.StatusID).OnDelete(DeleteBehavior.Restrict); e.HasOne<Address>().WithMany().HasForeignKey(x => x.AddressID).OnDelete(DeleteBehavior.Restrict); e.HasOne<AddressPVZ>().WithMany().HasForeignKey(x => x.AddressPVZID).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Product>(e => { e.ToTable("Product"); e.HasKey(x => x.ID); e.Property(x => x.Name).HasMaxLength(250); e.Property(x => x.Price).HasColumnType("decimal(18,0)"); e.Property(x => x.Image).HasMaxLength(250); e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Product_Order>(e => { e.ToTable("Product_Order"); e.HasKey(x => x.ID); e.Property(x => x.Price).HasColumnType("decimal(18,0)"); e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict); e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderID).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Role>(e => { e.ToTable("Role"); e.HasKey(x => x.ID); e.Property(x => x.Name).HasMaxLength(250); });
        modelBuilder.Entity<Status>(e => { e.ToTable("Status"); e.HasKey(x => x.ID); e.Property(x => x.Name).HasMaxLength(250); });
        modelBuilder.Entity<User>(e => { e.ToTable("User"); e.HasKey(x => x.ID); e.Property(x => x.Name).HasMaxLength(250); e.Property(x => x.Login).HasMaxLength(250); e.Property(x => x.Password).HasMaxLength(250); e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleID).OnDelete(DeleteBehavior.Restrict); });
    }
}
