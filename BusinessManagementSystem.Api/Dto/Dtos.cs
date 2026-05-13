namespace BusinessManagementSystem.Api.Dto;

public sealed record RegisterRequest(string FullName, string Email, string Password, string Role);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, int UserId, string EmployeeCode, string FullName, string Email, string Role, IEnumerable<string> Permissions);
public sealed record CreateUserRequest(string EmployeeCode, string FullName, string Email, string Password, string Role);

public sealed record UpsertSupplierRequest(string Name, string? ContactPerson, string? Phone, string? Email, string? Address);
public sealed record UpsertProductRequest(
    string Name,
    string Sku,
    int CategoryId,
    int? SupplierId,
    decimal CostPrice,
    decimal SellingPrice,
    int StockQuantity,
    int LowStockThreshold);

public sealed record CreateSaleRequest(string PaymentMethod, List<CreateSaleItemRequest> Items);
public sealed record CreateSaleItemRequest(int ProductId, int Quantity);
public sealed record SaleResponse(int Id, string InvoiceNumber, decimal TotalAmount, string PaymentMethod, DateTime SaleDate);

public sealed record DashboardSummary(
    decimal DailySales,
    decimal WeeklySales,
    decimal MonthlyRevenue,
    decimal GrossProfit,
    int ProductsInStock,
    int LowStockCount,
    int ActiveEmployees,
    int FailedLoginAttempts,
    IEnumerable<object> TopProducts,
    IEnumerable<object> StockAlerts,
    IEnumerable<object> EmployeeActivity,
    IEnumerable<object> Notifications);
