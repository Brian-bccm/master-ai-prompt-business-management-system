using BusinessManagementSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagementSystem.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<LoginLog> LoginLogs => Set<LoginLog>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SalesReport> SalesReports => Set<SalesReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });

        modelBuilder.Entity<User>().HasIndex(x => x.EmployeeCode).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasOne(x => x.RoleInfo)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Product>().Property(x => x.CostPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(x => x.SellingPrice).HasPrecision(18, 2);

        modelBuilder.Entity<Sale>().HasIndex(x => x.InvoiceNumber).IsUnique();
        modelBuilder.Entity<Sale>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Sale)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SaleItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<SaleItem>().Property(x => x.LineTotal).HasPrecision(18, 2);

        modelBuilder.Entity<Invoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(x => x.SaleId).IsUnique();

        modelBuilder.Entity<SalesReport>().Property(x => x.TotalSales).HasPrecision(18, 2);
        modelBuilder.Entity<SalesReport>().Property(x => x.GrossProfit).HasPrecision(18, 2);
    }
}
