using BusinessManagementSystem.Api.Data;
using BusinessManagementSystem.Api.Dto;
using BusinessManagementSystem.Api.Models;
using BusinessManagementSystem.Api.Security;
using BusinessManagementSystem.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<SalesService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<MonitoringService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DatabaseSeeder.Seed(db);
}

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
        new { email = "owner@bms.local", password = "Owner123!", role = Roles.Owner },
        new { email = "manager@bms.local", password = "Manager123!", role = Roles.Manager },
        new { email = "staff@bms.local", password = "Staff123!", role = Roles.Staff }
    }
}));

app.MapPost("/api/auth/register", (RegisterRequest request, AuthService auth) =>
    Results.Ok(auth.Register(request)));

app.MapPost("/api/auth/login", (LoginRequest request, HttpContext context, AuthService auth) =>
    Results.Ok(auth.Login(request, context)));

app.MapPost("/api/auth/logout", (HttpContext context, AuthService auth) =>
    Results.Ok(auth.Logout(CurrentUser(context))));

app.MapGet("/api/auth/me", (HttpContext context) =>
    Results.Ok(CurrentUser(context)));

app.MapGet("/api/users", (HttpContext context, AuthService auth, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.UsersManage, Permissions.EmployeesMonitor);
    return Results.Ok(auth.Users());
});

app.MapPost("/api/users", (CreateUserRequest request, HttpContext context, AuthService auth, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.UsersManage);
    return Results.Created("/api/users", auth.CreateUser(request, CurrentUser(context)));
});

app.MapGet("/api/roles", (HttpContext context, AppDbContext db, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.RolesManage, Permissions.EmployeesMonitor);
    return Results.Ok(db.Roles.AsNoTracking().OrderByDescending(r => r.Rank));
});

app.MapGet("/api/permissions", (HttpContext context, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.RolesManage, Permissions.EmployeesMonitor);
    return Results.Ok(permissions.Matrix());
});

app.MapGet("/api/categories", (AppDbContext db) =>
    Results.Ok(db.Categories.AsNoTracking().OrderBy(c => c.Name)));

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
    RequirePermission(context, context.RequestServices.GetRequiredService<PermissionService>(), Permissions.ProductsManage);
    return Results.Created($"/api/products", products.Create(request, CurrentUser(context)));
});

app.MapPut("/api/products/{id:int}", (int id, UpsertProductRequest request, HttpContext context, ProductService products) =>
{
    RequirePermission(context, context.RequestServices.GetRequiredService<PermissionService>(), Permissions.ProductsManage);
    return Results.Ok(products.Update(id, request, CurrentUser(context)));
});

app.MapDelete("/api/products/{id:int}", (int id, HttpContext context, ProductService products) =>
{
    RequirePermission(context, context.RequestServices.GetRequiredService<PermissionService>(), Permissions.ProductsManage);
    products.Delete(id, CurrentUser(context));
    return Results.NoContent();
});

app.MapGet("/api/suppliers", (SupplierService suppliers) =>
    Results.Ok(suppliers.GetAll()));

app.MapGet("/api/suppliers/{id:int}", (int id, SupplierService suppliers) =>
    Results.Ok(suppliers.GetById(id)));

app.MapPost("/api/suppliers", (UpsertSupplierRequest request, HttpContext context, SupplierService suppliers) =>
{
    RequirePermission(context, context.RequestServices.GetRequiredService<PermissionService>(), Permissions.ProductsManage);
    return Results.Created("/api/suppliers", suppliers.Create(request, CurrentUser(context)));
});

app.MapPut("/api/suppliers/{id:int}", (int id, UpsertSupplierRequest request, HttpContext context, SupplierService suppliers) =>
{
    RequirePermission(context, context.RequestServices.GetRequiredService<PermissionService>(), Permissions.ProductsManage);
    return Results.Ok(suppliers.Update(id, request, CurrentUser(context)));
});

app.MapDelete("/api/suppliers/{id:int}", (int id, HttpContext context, SupplierService suppliers) =>
{
    RequirePermission(context, context.RequestServices.GetRequiredService<PermissionService>(), Permissions.ProductsManage);
    suppliers.Delete(id, CurrentUser(context));
    return Results.NoContent();
});

app.MapGet("/api/sales", (DateTime? from, DateTime? to, SalesService sales) =>
    Results.Ok(sales.GetHistory(from, to)));

app.MapGet("/api/sales/date-range", (DateTime? from, DateTime? to, SalesService sales) =>
    Results.Ok(sales.GetHistory(from, to)));

app.MapGet("/api/sales/{id:int}", (int id, SalesService sales) =>
    Results.Ok(sales.GetById(id)));

app.MapPost("/api/sales", (CreateSaleRequest request, HttpContext context, SalesService sales, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.SalesCreate);
    return Results.Created("/api/sales", sales.Checkout(request, CurrentUser(context)));
});

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

app.MapGet("/api/dashboard/employee-activity", (HttpContext context, DashboardService dashboard, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.EmployeesMonitor);
    return Results.Ok(dashboard.GetSummary().EmployeeActivity);
});

app.MapGet("/api/audit-logs", (string? entityName, HttpContext context, AuditService audit, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.AuditView);
    return Results.Ok(audit.GetAll(entityName));
});

app.MapGet("/api/login-logs", (HttpContext context, MonitoringService monitoring, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.EmployeesMonitor);
    return Results.Ok(monitoring.LoginLogs());
});

app.MapGet("/api/employees/activity", (HttpContext context, MonitoringService monitoring, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.EmployeesMonitor);
    return Results.Ok(monitoring.EmployeeActivity());
});

app.MapGet("/api/notifications", (HttpContext context, NotificationService notifications, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.NotificationsView);
    return Results.Ok(notifications.GetAll());
});

app.MapGet("/api/reports/sales/export-excel", (ReportService reports) =>
    Results.File(System.Text.Encoding.UTF8.GetBytes(reports.SalesCsv()), "text/csv", "sales-report.csv"));

app.MapGet("/api/reports/inventory/export-excel", (ReportService reports) =>
    Results.File(System.Text.Encoding.UTF8.GetBytes(reports.InventoryCsv()), "text/csv", "inventory-report.csv"));

app.MapGet("/api/reports/employees/export-excel", (HttpContext context, ReportService reports, PermissionService permissions) =>
{
    RequirePermission(context, permissions, Permissions.EmployeesMonitor);
    return Results.File(System.Text.Encoding.UTF8.GetBytes(reports.EmployeeActivityCsv()), "text/csv", "employee-activity-report.csv");
});

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

static void RequirePermission(HttpContext context, PermissionService permissions, params string[] allowedPermissions)
{
    var user = CurrentUser(context);
    if (!allowedPermissions.Any(permission => permissions.Has(user, permission)))
    {
        throw new UnauthorizedAccessException("You do not have permission to perform this action.");
    }
}
