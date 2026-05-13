# Business Management System

Enterprise-style inventory, POS, employee monitoring, reporting, audit-log, invoice, and notification system built with ASP.NET Core Web API, SQL Server, Entity Framework Core, and vanilla HTML/CSS/JavaScript.

## Development: Visual Studio

Prerequisites:

- Visual Studio 2022 or newer
- .NET 9 SDK
- SQL Server Developer Edition, SQL Server Express, or LocalDB

Open the solution:

```text
BusinessManagementSystem.sln
```

Default development connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=BusinessManagementSystemDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Run from Visual Studio:

- Select `IIS Express`, `https`, or `http`
- Press `F5`
- Open `https://localhost:5001` or `http://localhost:5000`

Run from CLI:

```powershell
cd BusinessManagementSystem.Api
dotnet restore
dotnet ef database update
dotnet run --launch-profile https
```

Demo accounts seeded into SQL Server:

- Owner: `owner@bms.local` / `Owner123!`
- Manager: `manager@bms.local` / `Manager123!`
- Admin: `admin@bms.local` / `Admin123!`
- Staff: `staff@bms.local` / `Staff123!`

## Architecture

```text
wwwroot HTML/CSS/JS
        |
        v
ASP.NET Core API endpoints
        |
        v
Services with business workflows
        |
        v
Entity Framework Core DbContext
        |
        v
SQL Server
```

Core backend pieces:

- `Program.cs`: API routes, middleware, DI, startup migration/seed
- `Data/AppDbContext.cs`: EF Core SQL Server model
- `Data/DatabaseSeeder.cs`: roles, permissions, demo users, starter data
- `Services/BusinessServices.cs`: auth, permissions, inventory, sales, reports, monitoring, audit, notifications
- `Database/schema.sql`: SQL reference schema
- `Migrations/`: EF Core database migration

## Included Modules

- Multi-user authentication
- Owner / Manager / Admin / Staff role hierarchy
- Permission-based API checks
- Employee monitoring and login/logout tracking
- Failed login detection
- Full audit logging
- Inventory and suppliers
- POS checkout with automatic stock reduction
- Professional printable invoice
- Notifications for stock/security/sales events
- Sales, inventory, employee activity, and profit report data

## API Groups

```text
/api/auth
/api/users
/api/roles
/api/permissions
/api/products
/api/categories
/api/suppliers
/api/sales
/api/dashboard
/api/audit-logs
/api/login-logs
/api/employees/activity
/api/notifications
/api/reports
```

## Real Business Deployment: IIS + SQL Server

Server prerequisites:

- Windows Server
- IIS enabled
- ASP.NET Core Hosting Bundle for .NET 9
- SQL Server or remote SQL Server database

Deployment steps:

1. Create the production SQL Server database.
2. Copy `appsettings.Production.json.example` to `appsettings.Production.json`.
3. Set the production connection string and a long random `Auth:SigningKey`.
4. In Visual Studio, right-click `BusinessManagementSystem.Api` and choose `Publish`.
5. Use the `IIS-Folder` publish profile or create an IIS/Web Deploy profile.
6. Copy/publish the output to the IIS site folder.
7. Configure the IIS application pool:
   - No Managed Code
   - 64-bit enabled
8. Set `ASPNETCORE_ENVIRONMENT=Production`.
9. Run:

```powershell
dotnet ef database update
```

Production notes:

- Do not commit real production secrets.
- Use HTTPS only.
- Use a dedicated SQL login with least privilege.
- Back up the SQL Server database regularly.
- Replace demo accounts before real use.
