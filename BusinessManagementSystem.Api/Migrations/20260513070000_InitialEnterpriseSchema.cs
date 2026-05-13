using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessManagementSystem.Api.Migrations;

public partial class InitialEnterpriseSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Categories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Permissions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(nullable: false),
                Module = table.Column<string>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Permissions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(nullable: false),
                Rank = table.Column<int>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Roles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(nullable: false),
                ContactPerson = table.Column<string>(nullable: true),
                Phone = table.Column<string>(nullable: true),
                Email = table.Column<string>(nullable: true),
                Address = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Suppliers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Type = table.Column<string>(nullable: false),
                Title = table.Column<string>(nullable: false),
                Message = table.Column<string>(nullable: false),
                Severity = table.Column<string>(nullable: false),
                IsRead = table.Column<bool>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Notifications", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SalesReports",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                GrossProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SalesReports", x => x.Id));

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            columns: table => new
            {
                RoleId = table.Column<int>(nullable: false),
                PermissionId = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                table.ForeignKey("FK_RolePermissions_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_RolePermissions_Permissions_PermissionId", x => x.PermissionId, "Permissions", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                FullName = table.Column<string>(nullable: false),
                Email = table.Column<string>(nullable: false),
                PasswordHash = table.Column<string>(nullable: false),
                Role = table.Column<string>(nullable: false),
                RoleId = table.Column<int>(nullable: false),
                EmployeeCode = table.Column<string>(nullable: false),
                IsActive = table.Column<bool>(nullable: false),
                LastLoginAt = table.Column<DateTime>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey("FK_Users_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(nullable: false),
                Sku = table.Column<string>(nullable: false),
                CategoryId = table.Column<int>(nullable: false),
                SupplierId = table.Column<int>(nullable: true),
                CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                SellingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                StockQuantity = table.Column<int>(nullable: false),
                LowStockThreshold = table.Column<int>(nullable: false),
                IsActive = table.Column<bool>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.ForeignKey("FK_Products_Categories_CategoryId", x => x.CategoryId, "Categories", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Products_Suppliers_SupplierId", x => x.SupplierId, "Suppliers", "Id");
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(nullable: true),
                ActionType = table.Column<string>(nullable: false),
                EntityName = table.Column<string>(nullable: false),
                EntityId = table.Column<int>(nullable: true),
                Module = table.Column<string>(nullable: false),
                OldValue = table.Column<string>(nullable: true),
                NewValue = table.Column<string>(nullable: true),
                Description = table.Column<string>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
                table.ForeignKey("FK_AuditLogs_Users_UserId", x => x.UserId, "Users", "Id");
            });

        migrationBuilder.CreateTable(
            name: "LoginLogs",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(nullable: true),
                EmailAttempted = table.Column<string>(nullable: false),
                Status = table.Column<string>(nullable: false),
                IpAddress = table.Column<string>(nullable: false),
                UserAgent = table.Column<string>(nullable: false),
                LoginAt = table.Column<DateTime>(nullable: false),
                LogoutAt = table.Column<DateTime>(nullable: true),
                Reason = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoginLogs", x => x.Id);
                table.ForeignKey("FK_LoginLogs_Users_UserId", x => x.UserId, "Users", "Id");
            });

        migrationBuilder.CreateTable(
            name: "Sales",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                InvoiceNumber = table.Column<string>(nullable: false),
                UserId = table.Column<int>(nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PaymentMethod = table.Column<string>(nullable: false),
                SaleDate = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sales", x => x.Id);
                table.ForeignKey("FK_Sales_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SaleItems",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SaleId = table.Column<int>(nullable: false),
                ProductId = table.Column<int>(nullable: false),
                Quantity = table.Column<int>(nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SaleItems", x => x.Id);
                table.ForeignKey("FK_SaleItems_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_SaleItems_Sales_SaleId", x => x.SaleId, "Sales", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SaleId = table.Column<int>(nullable: false),
                InvoiceNumber = table.Column<string>(nullable: false),
                CompanyName = table.Column<string>(nullable: false),
                IssuedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
                table.ForeignKey("FK_Invoices_Sales_SaleId", x => x.SaleId, "Sales", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Permissions_Name", "Permissions", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Roles_Name", "Roles", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_RolePermissions_PermissionId", "RolePermissions", "PermissionId");
        migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Users_EmployeeCode", "Users", "EmployeeCode", unique: true);
        migrationBuilder.CreateIndex("IX_Users_RoleId", "Users", "RoleId");
        migrationBuilder.CreateIndex("IX_Products_CategoryId", "Products", "CategoryId");
        migrationBuilder.CreateIndex("IX_Products_Sku", "Products", "Sku", unique: true);
        migrationBuilder.CreateIndex("IX_Products_SupplierId", "Products", "SupplierId");
        migrationBuilder.CreateIndex("IX_AuditLogs_UserId", "AuditLogs", "UserId");
        migrationBuilder.CreateIndex("IX_LoginLogs_UserId", "LoginLogs", "UserId");
        migrationBuilder.CreateIndex("IX_Sales_InvoiceNumber", "Sales", "InvoiceNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Sales_UserId", "Sales", "UserId");
        migrationBuilder.CreateIndex("IX_SaleItems_ProductId", "SaleItems", "ProductId");
        migrationBuilder.CreateIndex("IX_SaleItems_SaleId", "SaleItems", "SaleId");
        migrationBuilder.CreateIndex("IX_Invoices_InvoiceNumber", "Invoices", "InvoiceNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Invoices_SaleId", "Invoices", "SaleId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AuditLogs");
        migrationBuilder.DropTable("Invoices");
        migrationBuilder.DropTable("LoginLogs");
        migrationBuilder.DropTable("Notifications");
        migrationBuilder.DropTable("RolePermissions");
        migrationBuilder.DropTable("SaleItems");
        migrationBuilder.DropTable("SalesReports");
        migrationBuilder.DropTable("Permissions");
        migrationBuilder.DropTable("Products");
        migrationBuilder.DropTable("Sales");
        migrationBuilder.DropTable("Categories");
        migrationBuilder.DropTable("Suppliers");
        migrationBuilder.DropTable("Users");
        migrationBuilder.DropTable("Roles");
    }
}
