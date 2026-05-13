namespace BusinessManagementSystem.Api.Models;

public static class Roles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
}

public static class Permissions
{
    public const string UsersManage = "users.manage";
    public const string EmployeesMonitor = "employees.monitor";
    public const string RolesManage = "roles.manage";
    public const string ProductsView = "products.view";
    public const string ProductsManage = "products.manage";
    public const string SalesCreate = "sales.create";
    public const string ReportsView = "reports.view";
    public const string AuditView = "audit.view";
    public const string NotificationsView = "notifications.view";
}

public sealed class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Rank { get; set; }
}

public sealed class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Module { get; set; } = "";
}

public sealed class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

public sealed class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Staff;
    public int RoleId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Role RoleInfo { get; set; } = null!;
}

public sealed class LoginLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string EmailAttempted { get; set; } = "";
    public string Status { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;
    public DateTime? LogoutAt { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Category Category { get; set; } = null!;
    public Supplier? Supplier { get; set; }
}

public sealed class Sale
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public List<SaleItem> Items { get; set; } = [];
    public User User { get; set; } = null!;
}

public sealed class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public sealed class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string ActionType { get; set; } = "";
    public string EntityName { get; set; } = "";
    public int? EntityId { get; set; }
    public string Module { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Notification
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Info";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Invoice
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public string CompanyName { get; set; } = "BMS Command";
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public Sale Sale { get; set; } = null!;
}

public sealed class SalesReport
{
    public int Id { get; set; }
    public DateOnly ReportDate { get; set; }
    public decimal TotalSales { get; set; }
    public decimal GrossProfit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
