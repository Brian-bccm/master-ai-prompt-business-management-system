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

## Real Business Deployment: Azure App Service + Azure SQL

Primary production target:

- Azure App Service
- Azure SQL Database
- GitHub Actions deployment from `main`

Deployment steps:

1. Create Azure SQL Server and Azure SQL Database.
2. Create Azure App Service with .NET 9 runtime.
3. Configure App Service environment variables:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `AUTH_SIGNING_KEY=<long-random-secret>`
4. Configure App Service connection string:
   - Name: `DefaultConnection`
   - Type: `SQLAzure`
   - Value: Azure SQL connection string
5. Add GitHub Actions secrets:
   - `AZURE_WEBAPP_NAME`
   - `AZURE_WEBAPP_PUBLISH_PROFILE`
   - `AZURE_SQL_CONNECTION_STRING`
   - `AUTH_SIGNING_KEY`
6. Push to `main`.

The deployment workflow uses `AZURE_WEBAPP_NAME` and `AZURE_WEBAPP_PUBLISH_PROFILE`.
Set `AZURE_SQL_CONNECTION_STRING` and `AUTH_SIGNING_KEY` in Azure App Service runtime configuration; keeping the same values in GitHub secrets is useful for future migration jobs.

The GitHub Actions workflow deploys the app to:

```text
https://<your-app-name>.azurewebsites.net
```

Database migrations:

- The app applies EF Core migrations on startup with `db.Database.Migrate()`.
- For stricter production later, move migrations into a separate release job.

Full Azure checklist:

```text
docs/azure-deployment-checklist.md
```

## Alternative Deployment: Windows IIS + SQL Server

For non-Azure Windows Server hosting:

1. Install the ASP.NET Core Hosting Bundle.
2. Create a SQL Server database.
3. Configure IIS App Pool as `No Managed Code`.
4. Publish with the `IIS-Folder` profile.
5. Set `ASPNETCORE_ENVIRONMENT=Production`.
6. Configure the production connection string and `AUTH_SIGNING_KEY`.
7. Run migrations if automatic startup migration is disabled:

```powershell
dotnet ef database update
```

Production notes:

- Do not commit real production secrets.
- Use HTTPS only.
- Use a dedicated SQL login with least privilege.
- Back up the SQL Server database regularly.
- Replace demo accounts before real use.
