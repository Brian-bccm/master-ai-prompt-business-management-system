using BusinessManagementSystem.Api.Models;
using BusinessManagementSystem.Api.Security;

namespace BusinessManagementSystem.Api.Data;

public static class DatabaseSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Roles.Any())
        {
            return;
        }

        var roles = new[]
        {
            new Role { Name = Roles.Owner, Rank = 100 },
            new Role { Name = Roles.Manager, Rank = 80 },
            new Role { Name = Roles.Admin, Rank = 70 },
            new Role { Name = Roles.Staff, Rank = 30 }
        };
        db.Roles.AddRange(roles);

        var permissions = new[]
        {
            NewPermission(Permissions.UsersManage, "Users"),
            NewPermission(Permissions.EmployeesMonitor, "Employees"),
            NewPermission(Permissions.RolesManage, "Security"),
            NewPermission(Permissions.ProductsView, "Inventory"),
            NewPermission(Permissions.ProductsManage, "Inventory"),
            NewPermission(Permissions.SalesCreate, "Sales"),
            NewPermission(Permissions.ReportsView, "Reports"),
            NewPermission(Permissions.AuditView, "Audit"),
            NewPermission(Permissions.NotificationsView, "Notifications")
        };
        db.Permissions.AddRange(permissions);
        db.SaveChanges();

        Grant(db, Roles.Owner, permissions.Select(p => p.Name).ToArray());
        Grant(db, Roles.Manager, Permissions.EmployeesMonitor, Permissions.ProductsView, Permissions.ProductsManage, Permissions.SalesCreate, Permissions.ReportsView, Permissions.AuditView, Permissions.NotificationsView);
        Grant(db, Roles.Admin, Permissions.ProductsView, Permissions.ProductsManage, Permissions.SalesCreate, Permissions.ReportsView, Permissions.NotificationsView);
        Grant(db, Roles.Staff, Permissions.ProductsView, Permissions.SalesCreate, Permissions.NotificationsView);

        db.Users.AddRange(
            NewUser(db, "OWN-001", "Business Owner", "owner@bms.local", "Owner123!", Roles.Owner),
            NewUser(db, "MGR-001", "Store Manager", "manager@bms.local", "Manager123!", Roles.Manager),
            NewUser(db, "ADM-001", "Inventory Admin", "admin@bms.local", "Admin123!", Roles.Admin),
            NewUser(db, "STF-001", "Sales Staff", "staff@bms.local", "Staff123!", Roles.Staff));

        db.Categories.AddRange(
            new Category { Name = "Electronics" },
            new Category { Name = "Stationery" },
            new Category { Name = "Grocery" });

        db.Suppliers.AddRange(
            new Supplier { Name = "North Star Supplies", ContactPerson = "Aina Rahman", Phone = "+60 12-222 1001", Email = "sales@northstar.test", Address = "Kuala Lumpur" },
            new Supplier { Name = "Metro Wholesale", ContactPerson = "Daniel Tan", Phone = "+60 12-222 1002", Email = "orders@metro.test", Address = "Petaling Jaya" });

        db.SaveChanges();

        db.Products.AddRange(
            new Product { Name = "Wireless Mouse", Sku = "ELE-MOU-001", CategoryId = 1, SupplierId = 1, CostPrice = 24.00m, SellingPrice = 49.90m, StockQuantity = 18, LowStockThreshold = 5 },
            new Product { Name = "USB-C Cable", Sku = "ELE-CAB-002", CategoryId = 1, SupplierId = 1, CostPrice = 7.50m, SellingPrice = 19.90m, StockQuantity = 4, LowStockThreshold = 8 },
            new Product { Name = "A4 Notebook", Sku = "STA-NOT-001", CategoryId = 2, SupplierId = 2, CostPrice = 2.10m, SellingPrice = 6.50m, StockQuantity = 48, LowStockThreshold = 10 },
            new Product { Name = "Premium Coffee Beans", Sku = "GRO-COF-001", CategoryId = 3, SupplierId = 2, CostPrice = 18.00m, SellingPrice = 39.90m, StockQuantity = 9, LowStockThreshold = 6 });

        db.AuditLogs.Add(new AuditLog { UserId = 1, ActionType = "SEED", EntityName = "System", Module = "System", Description = "Seeded SQL Server demo business data." });
        db.Notifications.AddRange(
            new Notification { Type = "Stock", Title = "USB-C Cable is low", Message = "Current stock is below threshold. Create a purchase order soon.", Severity = "Warning" },
            new Notification { Type = "Security", Title = "Failed login monitoring enabled", Message = "Repeated failed login attempts are tracked for managers.", Severity = "Info" });

        db.SaveChanges();
    }

    private static Permission NewPermission(string name, string module) => new() { Name = name, Module = module };

    private static User NewUser(AppDbContext db, string code, string name, string email, string password, string roleName)
    {
        var role = db.Roles.First(r => r.Name == roleName);
        return new User
        {
            EmployeeCode = code,
            FullName = name,
            Email = email,
            PasswordHash = PasswordHasher.Hash(password),
            Role = roleName,
            RoleId = role.Id
        };
    }

    private static void Grant(AppDbContext db, string roleName, params string[] permissionNames)
    {
        var role = db.Roles.First(r => r.Name == roleName);
        foreach (var permissionName in permissionNames)
        {
            var permission = db.Permissions.First(p => p.Name == permissionName);
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        }
        db.SaveChanges();
    }
}
