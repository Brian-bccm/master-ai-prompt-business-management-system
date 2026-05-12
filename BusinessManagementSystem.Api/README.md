# Business Management System

Production-style inventory, sales, admin, supplier, reporting, invoice, and audit-log system built with ASP.NET Core Web API and a vanilla HTML/CSS/JavaScript frontend.

## Run

```powershell
dotnet run --launch-profile https
```

Open:

- Frontend: `https://localhost:5001`
- API health: `https://localhost:5001/api`

Demo accounts:

- Admin: `admin@bms.local` / `Admin123!`
- Manager: `manager@bms.local` / `Manager123!`
- Staff: `staff@bms.local` / `Staff123!`

## Architecture

```text
wwwroot HTML/CSS/JS
        |
        v
ASP.NET Core API endpoints
        |
        v
Services with business rules
        |
        v
In-memory store for runnable demo
SQL Server schema in Database/schema.sql
```

The project is intentionally dependency-light so it runs immediately in this workspace. The included `Database/schema.sql` is the SQL Server schema for moving the demo store to a real database with repositories and EF Core.

## Included Modules

- Authentication with password hashing and signed bearer tokens
- Role-based access for Admin, Manager, and Staff
- Product inventory CRUD
- Category and supplier relationships
- Low stock alerts
- POS checkout with automatic stock reduction
- Invoice HTML print view
- Dashboard metrics
- Sales history
- CSV and simple PDF exports
- Audit logs for create, update, delete, and sales activity

## API Endpoints

```text
POST /api/auth/login
POST /api/auth/register
GET  /api/auth/me

GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}
GET    /api/products/low-stock
GET    /api/products/search?query=value

GET    /api/categories

GET    /api/suppliers
GET    /api/suppliers/{id}
POST   /api/suppliers
PUT    /api/suppliers/{id}
DELETE /api/suppliers/{id}

POST /api/sales
GET  /api/sales
GET  /api/sales/{id}
GET  /api/sales/date-range?from=2026-05-01&to=2026-05-12
GET  /api/sales/{id}/invoice

GET /api/dashboard/summary
GET /api/dashboard/daily-sales
GET /api/dashboard/monthly-revenue
GET /api/dashboard/top-products
GET /api/dashboard/stock-alerts

GET /api/audit-logs
GET /api/reports/sales/export-excel
GET /api/reports/sales/export-pdf
GET /api/reports/inventory/export-excel
GET /api/reports/inventory/export-pdf
```
