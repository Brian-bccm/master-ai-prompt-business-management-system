using System.Text;
using BusinessManagementSystem.Api.Data;
using BusinessManagementSystem.Api.Dto;
using BusinessManagementSystem.Api.Models;
using BusinessManagementSystem.Api.Security;

namespace BusinessManagementSystem.Api.Services;

public sealed class AuthService(AppStore store, TokenService tokenService)
{
    public AuthResponse Register(RegisterRequest request)
    {
        if (!IsAllowedRole(request.Role))
        {
            throw new InvalidOperationException("Role must be Admin, Manager, or Staff.");
        }

        return store.Mutate(s =>
        {
            if (s.Users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var user = new User
            {
                Id = s.NextUserId(),
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                PasswordHash = PasswordHasher.Hash(request.Password),
                Role = request.Role
            };

            s.Users.Add(user);
            s.AuditLogs.Add(new AuditLog { Id = s.NextAuditId(), UserId = user.Id, ActionType = "CREATE", EntityName = "Users", EntityId = user.Id, Description = "Registered user account." });
            return ToAuth(user);
        });
    }

    public AuthResponse Login(LoginRequest request)
    {
        var user = store.Users.FirstOrDefault(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase) && u.IsActive);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return ToAuth(user);
    }

    private AuthResponse ToAuth(User user) =>
        new(tokenService.Create(user), user.Id, user.FullName, user.Email, user.Role);

    private static bool IsAllowedRole(string role) => role is Roles.Admin or Roles.Manager or Roles.Staff;
}

public sealed class ProductService(AppStore store, AuditService audit)
{
    public IEnumerable<object> GetAll(string? query = null)
    {
        var products = store.Products.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(query))
        {
            products = products.Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return products.OrderBy(p => p.Name).Select(ToResponse);
    }

    public object GetById(int id)
    {
        var product = store.Products.FirstOrDefault(p => p.Id == id && p.IsActive);
        return product is null ? throw new KeyNotFoundException("Product not found.") : ToResponse(product);
    }

    public object Create(UpsertProductRequest request, AuthUser user) => store.Mutate(s =>
    {
        ValidateProduct(request, s);
        if (s.Products.Any(p => p.Sku.Equals(request.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("SKU already exists.");
        }

        var product = new Product
        {
            Id = s.NextProductId(),
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim().ToUpperInvariant(),
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            StockQuantity = request.StockQuantity,
            LowStockThreshold = request.LowStockThreshold
        };

        s.Products.Add(product);
        audit.Log(user.Id, "CREATE", "Products", product.Id, $"Created product {product.Name}.");
        return ToResponse(product);
    });

    public object Update(int id, UpsertProductRequest request, AuthUser user) => store.Mutate(s =>
    {
        ValidateProduct(request, s);
        var product = s.Products.FirstOrDefault(p => p.Id == id && p.IsActive) ?? throw new KeyNotFoundException("Product not found.");
        if (s.Products.Any(p => p.Id != id && p.Sku.Equals(request.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("SKU already exists.");
        }

        product.Name = request.Name.Trim();
        product.Sku = request.Sku.Trim().ToUpperInvariant();
        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.CostPrice = request.CostPrice;
        product.SellingPrice = request.SellingPrice;
        product.StockQuantity = request.StockQuantity;
        product.LowStockThreshold = request.LowStockThreshold;
        audit.Log(user.Id, "UPDATE", "Products", product.Id, $"Updated product {product.Name}.");
        return ToResponse(product);
    });

    public void Delete(int id, AuthUser user) => store.Mutate(s =>
    {
        var product = s.Products.FirstOrDefault(p => p.Id == id && p.IsActive) ?? throw new KeyNotFoundException("Product not found.");
        product.IsActive = false;
        audit.Log(user.Id, "DELETE", "Products", product.Id, $"Deactivated product {product.Name}.");
        return true;
    });

    public IEnumerable<object> LowStock() =>
        store.Products.Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold).Select(ToResponse);

    private object ToResponse(Product product)
    {
        var category = store.Categories.FirstOrDefault(c => c.Id == product.CategoryId)?.Name ?? "Uncategorized";
        var supplier = store.Suppliers.FirstOrDefault(s => s.Id == product.SupplierId)?.Name;
        return new
        {
            product.Id,
            product.Name,
            product.Sku,
            product.CategoryId,
            CategoryName = category,
            product.SupplierId,
            SupplierName = supplier,
            product.CostPrice,
            product.SellingPrice,
            product.StockQuantity,
            product.LowStockThreshold,
            IsLowStock = product.StockQuantity <= product.LowStockThreshold
        };
    }

    private static void ValidateProduct(UpsertProductRequest request, AppStore store)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Sku))
        {
            throw new InvalidOperationException("Product name and SKU are required.");
        }

        if (!store.Categories.Any(c => c.Id == request.CategoryId))
        {
            throw new InvalidOperationException("Category does not exist.");
        }

        if (request.SupplierId.HasValue && !store.Suppliers.Any(s => s.Id == request.SupplierId))
        {
            throw new InvalidOperationException("Supplier does not exist.");
        }
    }
}

public sealed class SupplierService(AppStore store, AuditService audit)
{
    public IEnumerable<Supplier> GetAll() => store.Suppliers.OrderBy(s => s.Name);
    public Supplier GetById(int id) => store.Suppliers.FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException("Supplier not found.");

    public Supplier Create(UpsertSupplierRequest request, AuthUser user) => store.Mutate(s =>
    {
        var supplier = new Supplier { Id = s.NextSupplierId(), Name = request.Name.Trim(), ContactPerson = request.ContactPerson, Phone = request.Phone, Email = request.Email, Address = request.Address };
        s.Suppliers.Add(supplier);
        audit.Log(user.Id, "CREATE", "Suppliers", supplier.Id, $"Created supplier {supplier.Name}.");
        return supplier;
    });

    public Supplier Update(int id, UpsertSupplierRequest request, AuthUser user) => store.Mutate(s =>
    {
        var supplier = s.Suppliers.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Supplier not found.");
        supplier.Name = request.Name.Trim();
        supplier.ContactPerson = request.ContactPerson;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        audit.Log(user.Id, "UPDATE", "Suppliers", supplier.Id, $"Updated supplier {supplier.Name}.");
        return supplier;
    });

    public void Delete(int id, AuthUser user) => store.Mutate(s =>
    {
        var supplier = s.Suppliers.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Supplier not found.");
        if (s.Products.Any(p => p.SupplierId == id && p.IsActive))
        {
            throw new InvalidOperationException("Cannot delete a supplier while active products are linked to it.");
        }

        s.Suppliers.Remove(supplier);
        audit.Log(user.Id, "DELETE", "Suppliers", supplier.Id, $"Deleted supplier {supplier.Name}.");
        return true;
    });
}

public sealed class SalesService(AppStore store, AuditService audit)
{
    public IEnumerable<object> GetHistory(DateTime? from = null, DateTime? to = null)
    {
        var sales = store.Sales.AsEnumerable();
        if (from.HasValue) sales = sales.Where(s => s.SaleDate.Date >= from.Value.Date);
        if (to.HasValue) sales = sales.Where(s => s.SaleDate.Date <= to.Value.Date);
        return sales.OrderByDescending(s => s.SaleDate).Select(ToResponse);
    }

    public object GetById(int id)
    {
        var sale = store.Sales.FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException("Sale not found.");
        return ToResponse(sale);
    }

    public SaleResponse Checkout(CreateSaleRequest request, AuthUser user) => store.Mutate(s =>
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Cart must contain at least one item.");
        }

        var sale = new Sale
        {
            Id = s.NextSaleId(),
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{s.Sales.Count + 1:000}",
            UserId = user.Id,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod,
            SaleDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var product = s.Products.FirstOrDefault(p => p.Id == item.ProductId && p.IsActive) ?? throw new KeyNotFoundException($"Product {item.ProductId} not found.");
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Quantity must be greater than zero.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for {product.Name}.");
            }

            product.StockQuantity -= item.Quantity;
            var lineTotal = product.SellingPrice * item.Quantity;
            sale.Items.Add(new SaleItem { Id = s.NextSaleItemId(), SaleId = sale.Id, ProductId = product.Id, Quantity = item.Quantity, UnitPrice = product.SellingPrice, LineTotal = lineTotal });
            sale.TotalAmount += lineTotal;
        }

        s.Sales.Add(sale);
        audit.Log(user.Id, "CREATE", "Sales", sale.Id, $"Created sale {sale.InvoiceNumber}.");
        return new SaleResponse(sale.Id, sale.InvoiceNumber, sale.TotalAmount, sale.PaymentMethod, sale.SaleDate);
    });

    public string InvoiceHtml(int id)
    {
        var sale = store.Sales.FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException("Sale not found.");
        var user = store.Users.FirstOrDefault(u => u.Id == sale.UserId)?.FullName ?? "Unknown";
        var rows = sale.Items.Select(item =>
        {
            var product = store.Products.FirstOrDefault(p => p.Id == item.ProductId)?.Name ?? "Product";
            return $"<tr><td>{product}</td><td>{item.Quantity}</td><td>{item.UnitPrice:C}</td><td>{item.LineTotal:C}</td></tr>";
        });

        return $$"""
        <!doctype html><html><head><title>{{sale.InvoiceNumber}}</title>
        <style>body{font-family:Arial;margin:40px;color:#111}table{width:100%;border-collapse:collapse}td,th{border-bottom:1px solid #ddd;padding:10px;text-align:left}.total{text-align:right;font-size:22px;font-weight:bold}</style></head>
        <body><h1>Invoice {{sale.InvoiceNumber}}</h1><p>Date: {{sale.SaleDate:u}}<br>Cashier: {{user}}<br>Payment: {{sale.PaymentMethod}}</p>
        <table><thead><tr><th>Product</th><th>Qty</th><th>Unit</th><th>Total</th></tr></thead><tbody>{{string.Join("", rows)}}</tbody></table>
        <p class="total">Total: {{sale.TotalAmount:C}}</p><script>window.print()</script></body></html>
        """;
    }

    private object ToResponse(Sale sale) => new
    {
        sale.Id,
        sale.InvoiceNumber,
        sale.UserId,
        Cashier = store.Users.FirstOrDefault(u => u.Id == sale.UserId)?.FullName,
        sale.TotalAmount,
        sale.PaymentMethod,
        sale.SaleDate,
        Items = sale.Items.Select(i => new
        {
            i.ProductId,
            ProductName = store.Products.FirstOrDefault(p => p.Id == i.ProductId)?.Name,
            i.Quantity,
            i.UnitPrice,
            i.LineTotal
        })
    };
}

public sealed class DashboardService(AppStore store)
{
    public DashboardSummary GetSummary()
    {
        var today = DateTime.UtcNow.Date;
        var month = new DateTime(today.Year, today.Month, 1);
        var topProducts = store.Sales.SelectMany(s => s.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductName = store.Products.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "Product",
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(5);

        var stockAlerts = store.Products
            .Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold)
            .Select(p => new { p.Id, p.Name, p.StockQuantity, p.LowStockThreshold });

        return new DashboardSummary(
            store.Sales.Where(s => s.SaleDate.Date == today).Sum(s => s.TotalAmount),
            store.Sales.Where(s => s.SaleDate >= month).Sum(s => s.TotalAmount),
            store.Products.Where(p => p.IsActive).Sum(p => p.StockQuantity),
            stockAlerts.Count(),
            topProducts,
            stockAlerts);
    }
}

public sealed class AuditService(AppStore store)
{
    public IEnumerable<object> GetAll(string? entityName = null)
    {
        var logs = store.AuditLogs.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(entityName))
        {
            logs = logs.Where(l => l.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
        }

        return logs.OrderByDescending(l => l.CreatedAt).Select(l => new
        {
            l.Id,
            l.UserId,
            UserName = store.Users.FirstOrDefault(u => u.Id == l.UserId)?.FullName,
            l.ActionType,
            l.EntityName,
            l.EntityId,
            l.Description,
            l.CreatedAt
        });
    }

    public void Log(int? userId, string actionType, string entityName, int? entityId, string description)
    {
        store.AuditLogs.Add(new AuditLog
        {
            Id = store.NextAuditId(),
            UserId = userId,
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            Description = description
        });
    }
}

public sealed class ReportService(AppStore store)
{
    public string SalesCsv()
    {
        var lines = new List<string> { "InvoiceNumber,SaleDate,PaymentMethod,TotalAmount" };
        lines.AddRange(store.Sales.OrderByDescending(s => s.SaleDate).Select(s => $"{s.InvoiceNumber},{s.SaleDate:u},{s.PaymentMethod},{s.TotalAmount:F2}"));
        return string.Join(Environment.NewLine, lines);
    }

    public string InventoryCsv()
    {
        var lines = new List<string> { "SKU,Name,Category,Supplier,StockQuantity,LowStockThreshold,SellingPrice" };
        lines.AddRange(store.Products.Where(p => p.IsActive).Select(p =>
        {
            var category = store.Categories.FirstOrDefault(c => c.Id == p.CategoryId)?.Name ?? "";
            var supplier = store.Suppliers.FirstOrDefault(s => s.Id == p.SupplierId)?.Name ?? "";
            return $"{p.Sku},{Escape(p.Name)},{Escape(category)},{Escape(supplier)},{p.StockQuantity},{p.LowStockThreshold},{p.SellingPrice:F2}";
        }));
        return string.Join(Environment.NewLine, lines);
    }

    public byte[] BasicPdf(string title, string body)
    {
        var text = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n4 0 obj<</Length 96>>stream\nBT /F1 18 Tf 72 720 Td ({title}) Tj /F1 10 Tf 0 -32 Td ({body[..Math.Min(body.Length, 70)]}) Tj ET\nendstream endobj\n5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\nxref\n0 6\n0000000000 65535 f \ntrailer<</Size 6/Root 1 0 R>>\nstartxref\n460\n%%EOF";
        return Encoding.ASCII.GetBytes(text);
    }

    private static string Escape(string value) => value.Contains(',') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
