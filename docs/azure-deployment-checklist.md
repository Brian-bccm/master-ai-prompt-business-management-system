# Azure App Service + Azure SQL Deployment Checklist

## 1. Azure resources

Create these resources in Azure Portal:

- Resource Group
- Azure SQL Server
- Azure SQL Database
- Azure App Service Plan
- Azure App Service

Recommended App Service settings:

- Runtime stack: `.NET 9`
- OS: `Windows`
- Always On: `On` if your App Service plan supports it
- HTTPS Only: `On`

## 2. Azure SQL Database

Create a SQL Server admin login and password.

Allow the App Service to connect:

- For simple setup, enable SQL Server firewall access for Azure services.
- For stricter production setup, configure private networking later.

Azure SQL connection string format:

```text
Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<admin-or-app-user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## 3. App Service configuration

In Azure Portal, open:

```text
App Service -> Settings -> Environment variables
```

Add application settings:

```text
ASPNETCORE_ENVIRONMENT = Production
AUTH_SIGNING_KEY = long-random-production-secret
```

Add connection string:

```text
Name: DefaultConnection
Type: SQLAzure
Value: Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

ASP.NET Core reads that connection string as:

```text
ConnectionStrings:DefaultConnection
```

The app also supports this fallback application setting:

```text
AZURE_SQL_CONNECTION_STRING
```

Use the App Service connection string setting first.

## 4. Database migrations

The app currently runs:

```csharp
db.Database.Migrate();
```

on startup. For this project stage, that is acceptable because:

- The schema is small
- There is one app instance
- It avoids manual migration mistakes

For stricter production later, remove automatic startup migration and run migrations through a controlled release job.

Manual migration option from a developer machine:

```powershell
cd BusinessManagementSystem.Api
dotnet ef database update --connection "Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

## 5. GitHub secrets

Add these in:

```text
GitHub repo -> Settings -> Secrets and variables -> Actions
```

Required for deployment workflow:

```text
AZURE_WEBAPP_NAME
AZURE_WEBAPP_PUBLISH_PROFILE
```

Required in Azure App Service runtime configuration, not required by the deploy workflow:

```text
AZURE_SQL_CONNECTION_STRING
AUTH_SIGNING_KEY
```

If you also want them stored in GitHub for reference or future migration jobs, add:

```text
AZURE_SQL_CONNECTION_STRING
AUTH_SIGNING_KEY
```

Do not commit real secrets to the repository.

## 6. Expected final URL

After deployment:

```text
https://<your-app-name>.azurewebsites.net
```

The App Service hosts both:

- ASP.NET Core API
- Static frontend files from `wwwroot`

No local PowerShell window is required in production.
