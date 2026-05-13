using System.Text;
using System.Text.Json;
using BusinessManagementSystem.Api.Data;
using BusinessManagementSystem.Api.Dto;
using BusinessManagementSystem.Api.Models;
using BusinessManagementSystem.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagementSystem.Api.Services;

public sealed class PermissionService(AppDbContext db)
{
    public IEnumerable<string> ForRole(string roleName)
    {
        var role = db.Roles.AsNoTracking().FirstOrDefault(r => r.Name == roleName);
        if (role is null) return [];

        var permissionIds = db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        return db.Permissions.AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Name)
            .OrderBy(p => p)
            .ToList();
    }

    public bool Has(AuthUser user, string permission) => ForRole(user.Role).Contains(permission);

    public IEnumerable<object> Matrix() => db.Roles.AsNoTracking()
        .OrderByDescending(r => r.Rank)
        .Select(role => new { role.Id, role.Name, role.Rank, Permissions = ForRole(role.Name) })
        .ToList();
}

public sealed class AuthService(AppDbContext db, TokenService tokenService, PermissionService permissions)
{
    public AuthResponse Register(RegisterRequest request) =>
        CreateUser(new CreateUserRequest("", request.FullName, request.Email, request.Password, request.Role), new AuthUser(1, "System", "system@bms.local", Roles.Owner));

    public AuthResponse CreateUser(CreateUserRequest request, AuthUser actor)
    {
        if (!permissions.Has(actor, Permissions.UsersManage))
            throw new UnauthorizedAccessException("You do not have permission to manage users.");

        if (!IsAllowedRole(request.Role))
            throw new InvalidOperationException("Role must be Owner, Manager, Admin, or Staff.");

        if (db.Users.Any(u => u.Email == request.Email.Trim().ToLower()))
            throw new InvalidOperationException("Email is already registered.");

        var role = db.Roles.First(r => r.Name == request.Role);
        var user = new User
        {
            EmployeeCode = string.IsNullOrWhiteSpace(request.EmployeeCode) ? $"EMP-{db.Users.Count() + 1:000}" : request.EmployeeCode.Trim().ToUpperInvariant(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role,
            RoleId = role.Id
        };

        db.Users.Add(user);
        db.SaveChanges();
        AddAudit(actor.Id, "CREATE", "Users", user.Id, "Users", null, SafeUser(user), $"Created employee account {user.EmployeeCode}.");
        return ToAuth(user);
    }

    public AuthResponse Login(LoginRequest request, HttpContext? context = null)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = db.Users.FirstOrDefault(u => u.Email == email);
        var ip = context?.Connection.RemoteIpAddress?.ToString() ?? "local";
        var userAgent = context?.Request.Headers.UserAgent.ToString() ?? "unknown";

        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            db.LoginLogs.Add(new LoginLog
            {
                UserId = user?.Id,
                EmailAttempted = request.Email,
                Status = "Failed",
                IpAddress = ip,
                UserAgent = userAgent,
                Reason = user is null ? "Unknown account" : "Invalid password or inactive account"
            });

            var recentFailures = db.LoginLogs.Count(l => l.Status == "Failed" && l.EmailAttempted == request.Email && l.LoginAt > DateTime.UtcNow.AddMinutes(-15)) + 1;
            if (recentFailures >= 3)
            {
                db.Notifications.Add(new Notification
                {
                    Type = "Security",
                    Title = "Suspicious login pattern",
                    Message = $"{recentFailures} failed login attempts for {request.Email} in the last 15 minutes.",
                    Severity = "Critical"
                });
            }

            db.SaveChanges();
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        db.LoginLogs.Add(new LoginLog { UserId = user.Id, EmailAttempted = request.Email, Status = "Success", IpAddress = ip, UserAgent = userAgent });
        db.SaveChanges();
        AddAudit(user.Id, "LOGIN", "Auth", user.Id, "Security", null, null, "User signed in.");
        return ToAuth(user);
    }

    public object Logout(AuthUser user)
    {
        var latest = db.LoginLogs
            .Where(l => l.UserId == user.Id && l.Status == "Success" && l.LogoutAt == null)
            .OrderByDescending(l => l.LoginAt)
            .FirstOrDefault();

        if (latest is not null) latest.LogoutAt = DateTime.UtcNow;
        db.SaveChanges();
        AddAudit(user.Id, "LOGOUT", "Auth", user.Id, "Security", null, null, "User signed out.");
        return new { message = "Logged out" };
    }

    public IEnumerable<object> Users() => db.Users.AsNoTracking()
        .OrderBy(u => u.FullName)
        .Select(u => new { u.Id, u.EmployeeCode, u.FullName, u.Email, u.Role, u.IsActive, u.LastLoginAt, u.CreatedAt })
        .ToList();

    private AuthResponse ToAuth(User user) =>
        new(tokenService.Create(user), user.Id, user.EmployeeCode, user.FullName, user.Email, user.Role, permissions.ForRole(user.Role));

    private static bool IsAllowedRole(string role) => role is Roles.Owner or Roles.Admin or Roles.Manager or Roles.Staff;
    private static object SafeUser(User user) => new { user.Id, user.EmployeeCode, user.FullName, user.Email, user.Role, user.IsActive };

    private void AddAudit(int? userId, string action, string entity, int? entityId, string module, object? oldValue, object? newValue, string description)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            ActionType = action,
            EntityName = entity,
            EntityId = entityId,
            Module = module,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
            Description = description
        });
        db.SaveChanges();
    }
}

public sealed class ProductService(AppDbContext db, AuditService audit, NotificationService notifications)
{
    public IEnumerable<object> GetAll(string? query = null)
    {
        var products = db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Supplier).Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(query))
            products = products.Where(p => p.Name.Contains(query) || p.Sku.Contains(query));
        return products.OrderBy(p => p.Name).Select(ToResponse).ToList();
    }

    public object GetById(int id)
    {
        var product = db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Supplier).FirstOrDefault(p => p.Id == id && p.IsActive);
        return product is null ? throw new KeyNotFoundException("Product not found.") : ToResponse(product);
    }

    public object Create(UpsertProductRequest request, AuthUser user)
    {
        ValidateProduct(request);
        if (db.Products.Any(p => p.Sku == request.Sku.Trim().ToUpper()))
            throw new InvalidOperationException("SKU already exists.");

        var product = new Product
        {
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim().ToUpperInvariant(),
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            StockQuantity = request.StockQuantity,
            LowStockThreshold = request.LowStockThreshold
        };

        db.Products.Add(product);
        db.SaveChanges();
        audit.Log(user.Id, "CREATE", "Products", product.Id, "Inventory", null, product, $"Created product {product.Name}.");
        return GetById(product.Id);
    }

    public object Update(int id, UpsertProductRequest request, AuthUser user)
    {
        ValidateProduct(request);
        var product = db.Products.FirstOrDefault(p => p.Id == id && p.IsActive) ?? throw new KeyNotFoundException("Product not found.");
        if (db.Products.Any(p => p.Id != id && p.Sku == request.Sku.Trim().ToUpper()))
            throw new InvalidOperationException("SKU already exists.");

        var before = new { product.Name, product.Sku, product.StockQuantity, product.SellingPrice };
        product.Name = request.Name.Trim();
        product.Sku = request.Sku.Trim().ToUpperInvariant();
        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.CostPrice = request.CostPrice;
        product.SellingPrice = request.SellingPrice;
        product.StockQuantity = request.StockQuantity;
        product.LowStockThreshold = request.LowStockThreshold;
        db.SaveChanges();
        audit.Log(user.Id, "UPDATE", "Products", product.Id, "Inventory", before, product, $"Updated product {product.Name}.");
        return GetById(product.Id);
    }

    public void Delete(int id, AuthUser user)
    {
        var product = db.Products.FirstOrDefault(p => p.Id == id && p.IsActive) ?? throw new KeyNotFoundException("Product not found.");
        var before = new { product.Id, product.Name, product.Sku };
        product.IsActive = false;
        db.SaveChanges();
        audit.Log(user.Id, "DELETE", "Products", product.Id, "Inventory", before, null, $"Deactivated product {product.Name}.");
    }

    public IEnumerable<object> LowStock() => db.Products.AsNoTracking()
        .Include(p => p.Category)
        .Include(p => p.Supplier)
        .Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold)
        .Select(ToResponse)
        .ToList();

    public void CheckLowStock(Product product)
    {
        if (product.StockQuantity <= product.LowStockThreshold)
            notifications.Create("Stock", "Low stock alert", $"{product.Name} is at {product.StockQuantity} units.", "Warning");
    }

    private object ToResponse(Product product) => new
    {
        product.Id,
        product.Name,
        product.Sku,
        product.CategoryId,
        CategoryName = product.Category?.Name ?? "Uncategorized",
        product.SupplierId,
        SupplierName = product.Supplier?.Name,
        product.CostPrice,
        product.SellingPrice,
        product.StockQuantity,
        product.LowStockThreshold,
        IsLowStock = product.StockQuantity <= product.LowStockThreshold
    };

    private void ValidateProduct(UpsertProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Sku))
            throw new InvalidOperationException("Product name and SKU are required.");
        if (!db.Categories.Any(c => c.Id == request.CategoryId))
            throw new InvalidOperationException("Category does not exist.");
        if (request.SupplierId.HasValue && !db.Suppliers.Any(s => s.Id == request.SupplierId))
            throw new InvalidOperationException("Supplier does not exist.");
    }
}

public sealed class SupplierService(AppDbContext db, AuditService audit)
{
    public IEnumerable<Supplier> GetAll() => db.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToList();
    public Supplier GetById(int id) => db.Suppliers.AsNoTracking().FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException("Supplier not found.");

    public Supplier Create(UpsertSupplierRequest request, AuthUser user)
    {
        var supplier = new Supplier { Name = request.Name.Trim(), ContactPerson = request.ContactPerson, Phone = request.Phone, Email = request.Email, Address = request.Address };
        db.Suppliers.Add(supplier);
        db.SaveChanges();
        audit.Log(user.Id, "CREATE", "Suppliers", supplier.Id, "Procurement", null, supplier, $"Created supplier {supplier.Name}.");
        return supplier;
    }

    public Supplier Update(int id, UpsertSupplierRequest request, AuthUser user)
    {
        var supplier = db.Suppliers.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Supplier not found.");
        var before = new { supplier.Name, supplier.ContactPerson, supplier.Phone, supplier.Email };
        supplier.Name = request.Name.Trim();
        supplier.ContactPerson = request.ContactPerson;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        db.SaveChanges();
        audit.Log(user.Id, "UPDATE", "Suppliers", supplier.Id, "Procurement", before, supplier, $"Updated supplier {supplier.Name}.");
        return supplier;
    }

    public void Delete(int id, AuthUser user)
    {
        var supplier = db.Suppliers.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Supplier not found.");
        if (db.Products.Any(p => p.SupplierId == id && p.IsActive))
            throw new InvalidOperationException("Cannot delete a supplier while active products are linked to it.");

        db.Suppliers.Remove(supplier);
        db.SaveChanges();
        audit.Log(user.Id, "DELETE", "Suppliers", supplier.Id, "Procurement", supplier, null, $"Deleted supplier {supplier.Name}.");
    }
}

public sealed class SalesService(AppDbContext db, AuditService audit)
{
    public IEnumerable<object> GetHistory(DateTime? from = null, DateTime? to = null)
    {
        var sales = db.Sales.AsNoTracking().Include(s => s.Items).ThenInclude(i => i.Product).Include(s => s.User).AsQueryable();
        if (from.HasValue) sales = sales.Where(s => s.SaleDate.Date >= from.Value.Date);
        if (to.HasValue) sales = sales.Where(s => s.SaleDate.Date <= to.Value.Date);
        return sales.OrderByDescending(s => s.SaleDate).Select(ToResponse).ToList();
    }

    public object GetById(int id)
    {
        var sale = db.Sales.AsNoTracking().Include(s => s.Items).ThenInclude(i => i.Product).Include(s => s.User).FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException("Sale not found.");
        return ToResponse(sale);
    }

    public SaleResponse Checkout(CreateSaleRequest request, AuthUser user)
    {
        if (request.Items.Count == 0) throw new InvalidOperationException("Cart must contain at least one item.");

        var sale = new Sale
        {
            InvoiceNumber = $"BMS-{DateTime.UtcNow:yyyyMMdd}-{db.Sales.Count() + 1:0000}",
            UserId = user.Id,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod,
            SaleDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var product = db.Products.FirstOrDefault(p => p.Id == item.ProductId && p.IsActive) ?? throw new KeyNotFoundException($"Product {item.ProductId} not found.");
            if (item.Quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");
            if (product.StockQuantity < item.Quantity) throw new InvalidOperationException($"Insufficient stock for {product.Name}.");

            product.StockQuantity -= item.Quantity;
            var lineTotal = product.SellingPrice * item.Quantity;
            sale.Items.Add(new SaleItem { ProductId = product.Id, Quantity = item.Quantity, UnitPrice = product.SellingPrice, LineTotal = lineTotal });
            sale.TotalAmount += lineTotal;

            if (product.StockQuantity <= product.LowStockThreshold)
                db.Notifications.Add(new Notification { Type = "Stock", Title = "Low stock alert", Message = $"{product.Name} is at {product.StockQuantity} units after checkout.", Severity = "Warning" });
        }

        db.Sales.Add(sale);
        db.Invoices.Add(new Invoice { Sale = sale, InvoiceNumber = sale.InvoiceNumber, CompanyName = "BMS Command" });
        db.Notifications.Add(new Notification { Type = "Sales", Title = "Sale completed", Message = $"{sale.InvoiceNumber} completed by {user.FullName} for {sale.TotalAmount:C}.", Severity = "Success" });
        db.SaveChanges();
        audit.Log(user.Id, "CREATE", "Sales", sale.Id, "Sales", null, sale, $"Created sale {sale.InvoiceNumber}.");
        return new SaleResponse(sale.Id, sale.InvoiceNumber, sale.TotalAmount, sale.PaymentMethod, sale.SaleDate);
    }

    public string InvoiceHtml(int id)
    {
        var sale = db.Sales.AsNoTracking().Include(s => s.Items).ThenInclude(i => i.Product).Include(s => s.User).FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException("Sale not found.");
        var rows = sale.Items.Select(item => $"<tr><td>{item.Product.Name}</td><td>{item.Quantity}</td><td>{item.UnitPrice:C}</td><td>{item.LineTotal:C}</td></tr>");

        return $$$"""
        <!doctype html><html><head><title>{{{sale.InvoiceNumber}}}</title>
        <style>body{font-family:Arial;margin:0;color:#111;background:#f4f7fb}.invoice{max-width:840px;margin:32px auto;background:white;padding:42px;border-radius:14px;box-shadow:0 20px 60px #0002}.brand{display:flex;justify-content:space-between;border-bottom:3px solid #0d9488;padding-bottom:18px}.brand h1{margin:0;color:#0f766e}table{width:100%;border-collapse:collapse;margin-top:28px}td,th{border-bottom:1px solid #ddd;padding:12px;text-align:left}.total{text-align:right;font-size:26px;font-weight:bold;color:#0f766e}.meta{color:#555;line-height:1.7}@media print{body{background:white}.invoice{box-shadow:none;margin:0}}</style></head>
        <body><main class="invoice"><section class="brand"><div><h1>BMS Command</h1><p>Professional Business Management System</p></div><strong>{{{sale.InvoiceNumber}}}</strong></section>
        <p class="meta">Timestamp: {{{sale.SaleDate:u}}}<br>Employee: {{{sale.User.FullName}}} ({{{sale.User.EmployeeCode}}})<br>Payment: {{{sale.PaymentMethod}}}</p>
        <table><thead><tr><th>Product</th><th>Qty</th><th>Unit</th><th>Total</th></tr></thead><tbody>{{{string.Join("", rows)}}}</tbody></table>
        <p class="total">Total: {{{sale.TotalAmount:C}}}</p><script>window.print()</script></main></body></html>
        """;
    }

    private object ToResponse(Sale sale) => new
    {
        sale.Id,
        sale.InvoiceNumber,
        sale.UserId,
        Cashier = sale.User?.FullName,
        sale.TotalAmount,
        sale.PaymentMethod,
        sale.SaleDate,
        Profit = sale.Items.Sum(i => (i.UnitPrice - i.Product.CostPrice) * i.Quantity),
        Items = sale.Items.Select(i => new { i.ProductId, ProductName = i.Product.Name, i.Quantity, i.UnitPrice, i.LineTotal })
    };
}

public sealed class DashboardService(AppDbContext db)
{
    public DashboardSummary GetSummary()
    {
        var today = DateTime.UtcNow.Date;
        var week = today.AddDays(-6);
        var month = new DateTime(today.Year, today.Month, 1);
        var saleItems = db.SaleItems.AsNoTracking().Include(i => i.Product).ToList();
        var stockAlerts = db.Products.AsNoTracking().Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold).Select(p => new { p.Id, p.Name, p.StockQuantity, p.LowStockThreshold }).ToList();

        var profit = saleItems.Sum(item => (item.UnitPrice - item.Product.CostPrice) * item.Quantity);

        return new DashboardSummary(
            db.Sales.Where(s => s.SaleDate.Date == today).Sum(s => s.TotalAmount),
            db.Sales.Where(s => s.SaleDate.Date >= week).Sum(s => s.TotalAmount),
            db.Sales.Where(s => s.SaleDate >= month).Sum(s => s.TotalAmount),
            profit,
            db.Products.Where(p => p.IsActive).Sum(p => p.StockQuantity),
            stockAlerts.Count,
            db.Users.Count(u => u.IsActive),
            db.LoginLogs.Count(l => l.Status == "Failed" && l.LoginAt > DateTime.UtcNow.AddDays(-1)),
            TopProducts(),
            stockAlerts,
            EmployeeActivity(),
            db.Notifications.AsNoTracking().OrderByDescending(n => n.CreatedAt).Take(5).Select(n => new { n.Type, n.Title, n.Message, n.Severity, n.CreatedAt }).ToList());
    }

    private IEnumerable<object> TopProducts() => db.SaleItems.AsNoTracking().Include(i => i.Product)
        .GroupBy(i => new { i.ProductId, i.Product.Name })
        .Select(g => new { ProductName = g.Key.Name, QuantitySold = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.LineTotal) })
        .OrderByDescending(x => x.QuantitySold)
        .Take(5)
        .ToList();

    private IEnumerable<object> EmployeeActivity() => db.Users.AsNoTracking().Select(u => new
    {
        u.EmployeeCode,
        u.FullName,
        u.Role,
        u.LastLoginAt,
        ActionsToday = db.AuditLogs.Count(a => a.UserId == u.Id && a.CreatedAt.Date == DateTime.UtcNow.Date),
        FailedLogins = db.LoginLogs.Count(l => l.UserId == u.Id && l.Status == "Failed")
    }).OrderByDescending(x => x.ActionsToday).Take(6).ToList();
}

public sealed class AuditService(AppDbContext db)
{
    public IEnumerable<object> GetAll(string? entityName = null)
    {
        var logs = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName))
            logs = logs.Where(l => l.EntityName == entityName || l.Module == entityName);

        return logs.OrderByDescending(l => l.CreatedAt).Select(l => new
        {
            l.Id,
            l.UserId,
            UserName = db.Users.Where(u => u.Id == l.UserId).Select(u => u.FullName).FirstOrDefault(),
            l.ActionType,
            l.Module,
            l.EntityName,
            l.EntityId,
            l.Description,
            l.OldValue,
            l.NewValue,
            l.CreatedAt
        }).ToList();
    }

    public void Log(int? userId, string actionType, string entityName, int? entityId, string module, object? oldValue, object? newValue, string description)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            Module = module,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
            Description = description
        });
        db.SaveChanges();
    }
}

public sealed class MonitoringService(AppDbContext db)
{
    public IEnumerable<object> LoginLogs() => db.LoginLogs.AsNoTracking().OrderByDescending(l => l.LoginAt).Select(l => new
    {
        l.Id,
        l.UserId,
        Employee = db.Users.Where(u => u.Id == l.UserId).Select(u => u.FullName).FirstOrDefault(),
        l.EmailAttempted,
        l.Status,
        l.IpAddress,
        l.LoginAt,
        l.LogoutAt,
        l.Reason
    }).ToList();

    public IEnumerable<object> EmployeeActivity() => db.Users.AsNoTracking().OrderBy(u => u.FullName).Select(u => new
    {
        u.Id,
        u.EmployeeCode,
        u.FullName,
        u.Email,
        u.Role,
        u.IsActive,
        u.LastLoginAt,
        SuccessfulLogins = db.LoginLogs.Count(l => l.UserId == u.Id && l.Status == "Success"),
        FailedLogins = db.LoginLogs.Count(l => l.UserId == u.Id && l.Status == "Failed"),
        AuditActions = db.AuditLogs.Count(a => a.UserId == u.Id)
    }).ToList();
}

public sealed class NotificationService(AppDbContext db)
{
    public IEnumerable<Notification> GetAll() => db.Notifications.AsNoTracking().OrderByDescending(n => n.CreatedAt).ToList();

    public Notification Create(string type, string title, string message, string severity)
    {
        var notification = new Notification { Type = type, Title = title, Message = message, Severity = severity };
        db.Notifications.Add(notification);
        db.SaveChanges();
        return notification;
    }
}

public sealed class ReportService(AppDbContext db)
{
    public string SalesCsv()
    {
        var sales = db.Sales.AsNoTracking().Include(s => s.Items).ThenInclude(i => i.Product).Include(s => s.User).OrderByDescending(s => s.SaleDate).ToList();
        var lines = new List<string> { "InvoiceNumber,SaleDate,Employee,PaymentMethod,TotalAmount,Profit" };
        lines.AddRange(sales.Select(s =>
        {
            var profit = s.Items.Sum(i => (i.UnitPrice - i.Product.CostPrice) * i.Quantity);
            return $"{s.InvoiceNumber},{s.SaleDate:u},{Escape(s.User.FullName)},{s.PaymentMethod},{s.TotalAmount:F2},{profit:F2}";
        }));
        return string.Join(Environment.NewLine, lines);
    }

    public string InventoryCsv()
    {
        var products = db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Supplier).Where(p => p.IsActive).ToList();
        var lines = new List<string> { "SKU,Name,Category,Supplier,StockQuantity,LowStockThreshold,SellingPrice,CostPrice" };
        lines.AddRange(products.Select(p => $"{p.Sku},{Escape(p.Name)},{Escape(p.Category.Name)},{Escape(p.Supplier?.Name ?? "")},{p.StockQuantity},{p.LowStockThreshold},{p.SellingPrice:F2},{p.CostPrice:F2}"));
        return string.Join(Environment.NewLine, lines);
    }

    public string EmployeeActivityCsv()
    {
        var users = db.Users.AsNoTracking().ToList();
        var lines = new List<string> { "EmployeeCode,FullName,Role,LastLoginAt,SuccessfulLogins,FailedLogins,AuditActions" };
        lines.AddRange(users.Select(u => $"{u.EmployeeCode},{Escape(u.FullName)},{u.Role},{u.LastLoginAt:u},{db.LoginLogs.Count(l => l.UserId == u.Id && l.Status == "Success")},{db.LoginLogs.Count(l => l.UserId == u.Id && l.Status == "Failed")},{db.AuditLogs.Count(a => a.UserId == u.Id)}"));
        return string.Join(Environment.NewLine, lines);
    }

    public byte[] BasicPdf(string title, string body)
    {
        var text = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n4 0 obj<</Length 96>>stream\nBT /F1 18 Tf 72 720 Td ({title}) Tj /F1 10 Tf 0 -32 Td ({body[..Math.Min(body.Length, 70)]}) Tj ET\nendstream endobj\n5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\nxref\n0 6\n0000000000 65535 f \ntrailer<</Size 6/Root 1 0 R>>\nstartxref\n460\n%%EOF";
        return Encoding.ASCII.GetBytes(text);
    }

    private static string Escape(string value) => value.Contains(',') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
