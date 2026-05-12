namespace BusinessManagementSystem.Api.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
}

public sealed class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Staff;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
}

public sealed class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string ActionType { get; set; } = "";
    public string EntityName { get; set; } = "";
    public int? EntityId { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
