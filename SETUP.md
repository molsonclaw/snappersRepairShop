# Snappers Repair Shop - Setup Instructions

## Overview
This is a production-ready Blazor Server application for managing an auto repair shop in Fountain City, WI.

## Features
- **MudBlazor UI**: Mobile-first, clean, modern interface
- **Entity Framework Core**: Azure SQL database support
- **Azure Blob Storage**: Secure photo storage with thumbnails
- **Identity with Roles**: Admin, Technician, Office roles
- **Billing Toggle**: Global BillingEnabled setting hides all price/cost fields when disabled

## Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB for development, Azure SQL for production)
- Azure Storage Account (optional for development - uses local emulator)

## Initial Setup

### 1. Restore NuGet Packages
```powershell
dotnet restore
```

### 2. Configure Database Connection
For development, the app uses LocalDB (already configured in `appsettings.Development.json`).

For production, update `appsettings.json` with your Azure SQL connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=SnappersRepairShop;..."
}
```

### 3. Configure Azure Blob Storage (Production)
Update `appsettings.json` with your Azure Storage account details:
```json
"AzureBlobStorage": {
  "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;...",
  "ContainerName": "job-photos"
}
```

For development, you can use Azurite (Azure Storage Emulator):
```powershell
# Install Azurite globally
npm install -g azurite

# Run Azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

### 4. Create and Apply Database Migrations
```powershell
# Add initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

### 5. Run the Application
```powershell
dotnet run
```

The application will:
- Automatically apply pending migrations
- Seed the three roles (Admin, Technician, Office)
- Initialize the blob storage container
- Create the singleton AppSettings row with BillingEnabled = false

### 6. Create First Admin User
1. Navigate to the application (usually https://localhost:5001)
2. Click "Register" to create your first user account
3. After registration, manually assign the Admin role using SQL:

```sql
-- Find the user ID
SELECT Id, UserName, Email FROM AspNetUsers;

-- Assign Admin role (replace USER_ID with actual ID)
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT 'USER_ID', Id FROM AspNetRoles WHERE Name = 'Admin';
```

Alternatively, you can use the .NET CLI:
```powershell
# This requires creating a custom command or using EF Core directly
```

## Project Structure

```
SnappersRepairShop/
├── Shared/
│   └── Models/           # All entity models
│       ├── Customer.cs
│       ├── Vehicle.cs
│       ├── WorkOrder.cs
│       ├── LaborLine.cs
│       ├── PartUsed.cs
│       ├── JobPhoto.cs
│       └── AppSettings.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Services/
│   └── BlobStorageService.cs
├── Pages/                # Razor pages and components
├── wwwroot/             # Static files
└── Program.cs           # Application startup

```

## Key Configuration Settings

### AppSettings (Database)
The `AppSettings` table contains a single row (ID = 1) with global settings:
- **BillingEnabled**: Master toggle for all billing features (default: false)
- **ShopName**: Display name for the shop
- **TaxRate**: Default tax rate (5.5% for Wisconsin)
- **DefaultLaborRate**: Default hourly labor rate ($95.00)

### User Roles
- **Admin**: Full access, can manage settings and users
- **Technician**: Can create/edit work orders, add labor/parts
- **Office**: Can manage customers, vehicles, and view work orders

## Next Steps

1. **Scaffold Identity Pages** (optional - for customization):
   ```powershell
   dotnet aspnet-codegenerator identity -dc ApplicationDbContext
   ```

2. **Build Work Order Pages**: Create CRUD pages for work orders
3. **Build Customer/Vehicle Pages**: Create CRUD pages for customers and vehicles
4. **Build Settings Page**: Admin page to toggle BillingEnabled and configure shop details
5. **Implement Photo Upload**: Use BlobStorageService in work order detail pages

## Development Notes

- The application uses server-side Blazor (not WebAssembly)
- All price/cost fields are hidden when `BillingEnabled = false`
- Photos are stored in Azure Blob Storage with private access (SAS URLs for viewing)
- Thumbnails are automatically generated on upload
- The app is mobile-first with responsive MudBlazor components

## Troubleshooting

### Database Connection Issues
- Ensure SQL Server LocalDB is installed
- Check connection string in appsettings.Development.json
- Verify migrations have been applied: `dotnet ef database update`

### Azure Blob Storage Issues
- For development, use Azurite or set ConnectionString to "UseDevelopmentStorage=true"
- Ensure the container name matches in configuration
- Check that the storage account has proper permissions

### Identity/Login Issues
- Ensure migrations have been applied (creates Identity tables)
- Verify roles were seeded (check AspNetRoles table)
- Check that cookies are enabled in browser

## Production Deployment

1. Update `appsettings.json` with production connection strings
2. Set `ASPNETCORE_ENVIRONMENT=Production`
3. Publish the application: `dotnet publish -c Release`
4. Deploy to Azure App Service or IIS
5. Ensure Azure SQL firewall allows your app service IP
6. Configure Azure Blob Storage CORS if needed

## Support

For issues or questions, contact the development team.

