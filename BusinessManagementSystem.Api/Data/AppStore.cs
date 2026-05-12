using BusinessManagementSystem.Api.Models;
using BusinessManagementSystem.Api.Security;

namespace BusinessManagementSystem.Api.Data;

public sealed class AppStore
{
    private readonly object _lock = new();
    private int _nextUserId = 1;
    private int _nextCategoryId = 1;
    private int _nextSupplierId = 1;
    private int _nextProductId = 1;
    private int _nextSaleId = 1;
    private int _nextSaleItemId = 1;
    private int _nextAuditId = 1;

    public List<User> Users { get; } = [];
    public List<Category> Categories { get; } = [];
    public List<Supplier> Suppliers { get; } = [];
    public List<Product> Products { get; } = [];
    public List<Sale> Sales { get; } = [];
    public List<AuditLog> AuditLogs { get; } = [];

    public AppStore()
    {
        Seed();
    }

    public T Mutate<T>(Func<AppStore, T> action)
    {
        lock (_lock)
        {
            return action(this);
        }
    }

    public int NextUserId() => _nextUserId++;
    public int NextCategoryId() => _nextCategoryId++;
    public int NextSupplierId() => _nextSupplierId++;
    public int NextProductId() => _nextProductId++;
    public int NextSaleId() => _nextSaleId++;
    public int NextSaleItemId() => _nextSaleItemId++;
    public int NextAuditId() => _nextAuditId++;

    private void Seed()
    {
        Users.AddRange([
            new User { Id = NextUserId(), FullName = "System Admin", Email = "admin@bms.local", PasswordHash = PasswordHasher.Hash("Admin123!"), Role = Roles.Admin },
            new User { Id = NextUserId(), FullName = "Store Manager", Email = "manager@bms.local", PasswordHash = PasswordHasher.Hash("Manager123!"), Role = Roles.Manager },
            new User { Id = NextUserId(), FullName = "Sales Staff", Email = "staff@bms.local", PasswordHash = PasswordHasher.Hash("Staff123!"), Role = Roles.Staff }
        ]);

        Categories.AddRange([
            new Category { Id = NextCategoryId(), Name = "Electronics" },
            new Category { Id = NextCategoryId(), Name = "Stationery" },
            new Category { Id = NextCategoryId(), Name = "Grocery" }
        ]);

        Suppliers.AddRange([
            new Supplier { Id = NextSupplierId(), Name = "North Star Supplies", ContactPerson = "Aina Rahman", Phone = "+60 12-222 1001", Email = "sales@northstar.test", Address = "Kuala Lumpur" },
            new Supplier { Id = NextSupplierId(), Name = "Metro Wholesale", ContactPerson = "Daniel Tan", Phone = "+60 12-222 1002", Email = "orders@metro.test", Address = "Petaling Jaya" }
        ]);

        Products.AddRange([
            new Product { Id = NextProductId(), Name = "Wireless Mouse", Sku = "ELE-MOU-001", CategoryId = 1, SupplierId = 1, CostPrice = 24.00m, SellingPrice = 49.90m, StockQuantity = 18, LowStockThreshold = 5 },
            new Product { Id = NextProductId(), Name = "USB-C Cable", Sku = "ELE-CAB-002", CategoryId = 1, SupplierId = 1, CostPrice = 7.50m, SellingPrice = 19.90m, StockQuantity = 4, LowStockThreshold = 8 },
            new Product { Id = NextProductId(), Name = "A4 Notebook", Sku = "STA-NOT-001", CategoryId = 2, SupplierId = 2, CostPrice = 2.10m, SellingPrice = 6.50m, StockQuantity = 48, LowStockThreshold = 10 },
            new Product { Id = NextProductId(), Name = "Premium Coffee Beans", Sku = "GRO-COF-001", CategoryId = 3, SupplierId = 2, CostPrice = 18.00m, SellingPrice = 39.90m, StockQuantity = 9, LowStockThreshold = 6 }
        ]);

        AuditLogs.Add(new AuditLog
        {
            Id = NextAuditId(),
            UserId = 1,
            ActionType = "SEED",
            EntityName = "System",
            Description = "Seeded demo business data."
        });
    }
}
