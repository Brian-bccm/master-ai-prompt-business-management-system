using BusinessManagementSystem.Api.Data;
using BusinessManagementSystem.Api.Dto;
using BusinessManagementSystem.Api.Models;
using BusinessManagementSystem.Api.Security;
using BusinessManagementSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AppStore>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<AuditService>();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<SupplierService>();
builder.Services.AddSingleton<SalesService>();
builder.Services.AddSingleton<DashboardService>();
builder.Services.AddSingleton<ReportService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path == "/api/auth/login" ||
        context.Request.Path == "/api/auth/register" ||
        context.Request.Path == "/api")
    {
        await next();
        return;
    }

    var tokenService = context.RequestServices.GetRequiredService<TokenService>();
    var authHeader = context.Request.Headers.Authorization.ToString();
    var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? authHeader["Bearer ".Length..]
        : "";

    var user = tokenService.Validate(token);
    if (user is null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = "Authentication required." });
        return;
    }

    context.Items["User"] = user;
    await next();
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = error switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        await context.Response.WriteAsJsonAsync(new
        {
            message = error?.Message ?? "Unexpected server error."
        });
    });
});

app.MapGet("/api", () => Results.Ok(new
{
    name = "Business Management System API",
    status = "running",
    frontend = "/index.html",
    demoAccounts = new[]
    {
        new { email = "admin@bms.local", password = "Admin123!", role = Roles.Admin },
        new { email = "manager@bms.local", password = "Manager123!", role = Roles.Manager },
        new { email = "staff@bms.local", password = "Staff123!", role = Roles.Staff }
    }
}));

app.MapPost("/api/auth/register", (RegisterRequest request, AuthService auth) =>
    Results.Ok(auth.Register(request)));

app.MapPost("/api/auth/login", (LoginRequest request, AuthService auth) =>
    Results.Ok(auth.Login(request)));

app.MapGet("/api/auth/me", (HttpContext context) =>
    Results.Ok(CurrentUser(context)));

app.MapGet("/api/categories", (AppStore store) =>
    Results.Ok(store.Categories.OrderBy(c => c.Name)));

app.MapGet("/api/products", (string? query, ProductService products) =>
    Results.Ok(products.GetAll(query)));

app.MapGet("/api/products/search", (string query, ProductService products) =>
    Results.Ok(products.GetAll(query)));

app.MapGet("/api/products/low-stock", (ProductService products) =>
    Results.Ok(products.LowStock()));

app.MapGet("/api/products/{id:int}", (int id, ProductService products) =>
    Results.Ok(products.GetById(id)));

app.MapPost("/api/products", (UpsertProductRequest request, HttpContext context, ProductService products) =>
{
    RequireRole(context, Roles.Admin, Roles.Manager);
    return Results.Created($"/api/products", products.Create(request, CurrentUser(context)));
});

app.MapPut("/api/products/{id:int}", (int id, UpsertProductRequest request, HttpContext context, ProductService products) =>
{
    RequireRole(context, Roles.Admin, Roles.Manager);
    return Results.Ok(products.Update(id, request, CurrentUser(context)));
});

app.MapDelete("/api/products/{id:int}", (int id, HttpContext context, ProductService products) =>
{
    RequireRole(context, Roles.Admin);
    products.Delete(id, CurrentUser(context));
    return Results.NoContent();
});

app.MapGet("/api/suppliers", (SupplierService suppliers) =>
    Results.Ok(suppliers.GetAll()));

app.MapGet("/api/suppliers/{id:int}", (int id, SupplierService suppliers) =>
    Results.Ok(suppliers.GetById(id)));

app.MapPost("/api/suppliers", (UpsertSupplierRequest request, HttpContext context, SupplierService suppliers) =>
{
    RequireRole(context, Roles.Admin, Roles.Manager);
    return Results.Created("/api/suppliers", suppliers.Create(request, CurrentUser(context)));
});

app.MapPut("/api/suppliers/{id:int}", (int id, UpsertSupplierRequest request, HttpContext context, SupplierService suppliers) =>
{
    RequireRole(context, Roles.Admin, Roles.Manager);
    return Results.Ok(suppliers.Update(id, request, CurrentUser(context)));
});

app.MapDelete("/api/suppliers/{id:int}", (int id, HttpContext context, SupplierService suppliers) =>
{
    RequireRole(context, Roles.Admin);
    suppliers.Delete(id, CurrentUser(context));
    return Results.NoContent();
});

app.MapGet("/api/sales", (DateTime? from, DateTime? to, SalesService sales) =>
    Results.Ok(sales.GetHistory(from, to)));

app.MapGet("/api/sales/date-range", (DateTime? from, DateTime? to, SalesService sales) =>
    Results.Ok(sales.GetHistory(from, to)));

app.MapGet("/api/sales/{id:int}", (int id, SalesService sales) =>
    Results.Ok(sales.GetById(id)));

app.MapPost("/api/sales", (CreateSaleRequest request, HttpContext context, SalesService sales) =>
    Results.Created("/api/sales", sales.Checkout(request, CurrentUser(context))));

app.MapGet("/api/sales/{id:int}/invoice", (int id, SalesService sales) =>
    Results.Content(sales.InvoiceHtml(id), "text/html"));

app.MapGet("/api/dashboard/summary", (DashboardService dashboard) =>
    Results.Ok(dashboard.GetSummary()));

app.MapGet("/api/dashboard/daily-sales", (DashboardService dashboard) =>
    Results.Ok(new { value = dashboard.GetSummary().DailySales }));

app.MapGet("/api/dashboard/monthly-revenue", (DashboardService dashboard) =>
    Results.Ok(new { value = dashboard.GetSummary().MonthlyRevenue }));

app.MapGet("/api/dashboard/top-products", (DashboardService dashboard) =>
    Results.Ok(dashboard.GetSummary().TopProducts));

app.MapGet("/api/dashboard/stock-alerts", (DashboardService dashboard) =>
    Results.Ok(dashboard.GetSummary().StockAlerts));

app.MapGet("/api/audit-logs", (string? entityName, AuditService audit) =>
    Results.Ok(audit.GetAll(entityName)));

app.MapGet("/api/reports/sales/export-excel", (ReportService reports) =>
    Results.File(System.Text.Encoding.UTF8.GetBytes(reports.SalesCsv()), "text/csv", "sales-report.csv"));

app.MapGet("/api/reports/inventory/export-excel", (ReportService reports) =>
    Results.File(System.Text.Encoding.UTF8.GetBytes(reports.InventoryCsv()), "text/csv", "inventory-report.csv"));

app.MapGet("/api/reports/sales/export-pdf", (ReportService reports) =>
    Results.File(reports.BasicPdf("Sales Report", reports.SalesCsv()), "application/pdf", "sales-report.pdf"));

app.MapGet("/api/reports/inventory/export-pdf", (ReportService reports) =>
    Results.File(reports.BasicPdf("Inventory Report", reports.InventoryCsv()), "application/pdf", "inventory-report.pdf"));

app.MapFallbackToFile("index.html");

app.Run();

static AuthUser CurrentUser(HttpContext context) =>
    context.Items.TryGetValue("User", out var user) && user is AuthUser authUser
        ? authUser
        : throw new UnauthorizedAccessException("Authentication required.");

static void RequireRole(HttpContext context, params string[] roles)
{
    var user = CurrentUser(context);
    if (!roles.Contains(user.Role))
    {
        throw new UnauthorizedAccessException("You do not have permission to perform this action.");
    }
}
