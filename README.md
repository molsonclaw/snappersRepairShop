# Snappers Repair Shop

A production-ready Blazor Server application for managing a small auto repair shop in Fountain City, WI. Built with .NET 9, MudBlazor, Entity Framework Core, and Azure services.

## 🎯 Features

- **Mobile-First Design**: Optimized for tablets in shop bays and mobile devices
- **Work Order Management**: Easy work entry, parts tracking, labor tracking
- **Photo Management**: Camera capture, automatic compression (<2MB), Azure Blob Storage
- **Role-Based Access**: Admin, Technician, and Office roles with granular permissions
- **Billing Toggle**: Global setting to hide/show all price fields throughout the app
- **Real-Time Updates**: SignalR integration for live dashboard and job list updates
- **Customer & Vehicle Management**: Quick add dialogs, license plate search
- **Responsive UI**: MudBlazor components with custom CSS for shop bay tablets

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (LocalDB for development, Azure SQL for production)
- Azure Storage Account (optional for development)

## 🚀 Quick Start (Development)

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Snappers
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Update Database Connection (Optional)

The app uses LocalDB by default. To use a different SQL Server instance, update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=SnappersRepairShop;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 4. Apply Database Migrations

```bash
dotnet ef database update
```

This will:
- Create the database schema
- Seed default roles (Admin, Technician, Office)
- Create default admin user: `admin@snappersrepair.com` / `Admin@123`
- Create default AppSettings with `BillingEnabled = false`

### 5. Run the Application

```bash
dotnet run
```

The application will be available at:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5002

### 6. Login

Use the default admin credentials:
- **Email**: `admin@snappersrepair.com`
- **Password**: `Admin@123`

**⚠️ IMPORTANT**: Change the default admin password immediately in production!

## 🔧 Configuration

### Blob Storage (Optional for Development)

For development, the app uses `UseDevelopmentStorage=true` which requires [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite).

To skip blob storage in development, the app will automatically disable photo uploads if blob storage is not configured.

### Application Settings

Key settings in `appsettings.json`:

```json
{
  "AzureBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "job-photos",
    "ThumbnailWidth": 300,
    "ThumbnailHeight": 300
  }
}
```

## 📱 Mobile & Tablet Optimization

The application is optimized for:
- **Mobile phones** (320px - 767px): Touch-friendly buttons, simplified layouts
- **Shop bay tablets** (768px - 1024px): Larger text, increased spacing, easy-to-tap controls
- **Desktop** (1025px+): Full feature set with multi-column layouts

### Recommended Devices
- iPad (10.2" or larger) for shop bay use
- Any modern smartphone for mobile technicians

## 👥 User Roles

### Admin
- Full access to all features
- Can view/edit all work orders
- Access to Settings page
- Can manage users
- Sees billing fields (when enabled)

### Technician
- Can create/edit only their own work orders
- Can view and upload photos
- **Never** sees billing fields (regardless of BillingEnabled setting)
- Cannot access Settings page

### Office
- Can view all work orders
- Can manage customers and vehicles
- Sees billing fields (when enabled)
- Cannot create/edit work orders
- Cannot access Settings page

## 💰 Billing Features Toggle

The application includes a global `BillingEnabled` setting that controls visibility of all price/cost fields.

### How to Toggle Billing

1. Login as Admin
2. Navigate to **Settings** page
3. Toggle the **"Enable Billing Features"** switch
4. Click **Save**

### What Happens When Billing is Enabled

- All price/cost fields become visible (except for Technicians)
- Existing work orders are automatically backfilled with calculated totals:
  - `LaborTotal` = Sum of (Hours × Rate)
  - `PartsTotal` = Sum of (Quantity × Unit Cost)
- Changes propagate instantly across all pages via SignalR

### What Happens When Billing is Disabled

- All price/cost fields are hidden throughout the app
- Work orders can still be created and managed
- Billing data is preserved in the database (not deleted)

## 🌐 Azure Deployment

### Prerequisites

- Azure subscription
- Azure CLI installed
- Resource group created

### Step 1: Create Azure Resources

#### Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name snappers-sql-server \
  --resource-group snappers-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password YOUR_SECURE_PASSWORD

# Create Database
az sql db create \
  --resource-group snappers-rg \
  --server snappers-sql-server \
  --name SnappersRepairShop \
  --service-objective S0

# Configure firewall (allow Azure services)
az sql server firewall-rule create \
  --resource-group snappers-rg \
  --server snappers-sql-server \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

#### Create Azure Storage Account

```bash
# Create storage account
az storage account create \
  --name snappersstorage \
  --resource-group snappers-rg \
  --location eastus \
  --sku Standard_LRS \
  --kind StorageV2

# Create blob container
az storage container create \
  --name job-photos \
  --account-name snappersstorage \
  --public-access off
```

#### Create Azure App Service

```bash
# Create App Service Plan
az appservice plan create \
  --name snappers-plan \
  --resource-group snappers-rg \
  --location eastus \
  --sku B1 \
  --is-linux false

# Create Web App
az webapp create \
  --name snappers-repair-shop \
  --resource-group snappers-rg \
  --plan snappers-plan \
  --runtime "DOTNET|9.0"
```

### Step 2: Configure Application Settings

```bash
# Get SQL connection string
SQL_CONNECTION_STRING="Server=tcp:snappers-sql-server.database.windows.net,1433;Initial Catalog=SnappersRepairShop;Persist Security Info=False;User ID=sqladmin;Password=YOUR_SECURE_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Get storage connection string
STORAGE_CONNECTION_STRING=$(az storage account show-connection-string \
  --name snappersstorage \
  --resource-group snappers-rg \
  --query connectionString \
  --output tsv)

# Configure app settings
az webapp config appsettings set \
  --name snappers-repair-shop \
  --resource-group snappers-rg \
  --settings \
    "ConnectionStrings__DefaultConnection=$SQL_CONNECTION_STRING" \
    "AzureBlobStorage__ConnectionString=$STORAGE_CONNECTION_STRING" \
    "AzureBlobStorage__ContainerName=job-photos" \
    "AzureBlobStorage__ThumbnailWidth=300" \
    "AzureBlobStorage__ThumbnailHeight=300"
```

### Step 3: Deploy the Application

#### Option A: Deploy from Local Machine

```bash
# Publish the app
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group snappers-rg \
  --name snappers-repair-shop \
  --src deploy.zip
```

#### Option B: Deploy from GitHub Actions

Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Publish
      run: dotnet publish -c Release -o ./publish

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'snappers-repair-shop'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### Step 4: Run Database Migrations

After deployment, run migrations:

```bash
# Install EF Core tools globally (if not already installed)
dotnet tool install --global dotnet-ef

# Run migrations against Azure SQL
dotnet ef database update --connection "$SQL_CONNECTION_STRING"
```

### Step 5: Verify Deployment

1. Navigate to your app URL: `https://snappers-repair-shop.azurewebsites.net`
2. Login with default admin credentials
3. Change the admin password immediately
4. Create additional users as needed

## 🔐 Security Considerations

### Production Checklist

- [ ] Change default admin password
- [ ] Enable HTTPS only (disable HTTP)
- [ ] Configure Azure SQL firewall rules (restrict to App Service IP)
- [ ] Enable Azure SQL Advanced Threat Protection
- [ ] Configure Application Insights for monitoring
- [ ] Set up automated backups for Azure SQL
- [ ] Enable Azure Blob Storage soft delete
- [ ] Review and restrict CORS settings
- [ ] Configure custom domain with SSL certificate
- [ ] Set up Azure Key Vault for secrets management

### Recommended: Use Azure Key Vault

```bash
# Create Key Vault
az keyvault create \
  --name snappers-keyvault \
  --resource-group snappers-rg \
  --location eastus

# Add secrets
az keyvault secret set \
  --vault-name snappers-keyvault \
  --name "SqlConnectionString" \
  --value "$SQL_CONNECTION_STRING"

az keyvault secret set \
  --vault-name snappers-keyvault \
  --name "BlobStorageConnectionString" \
  --value "$STORAGE_CONNECTION_STRING"

# Grant App Service access to Key Vault
# (Enable managed identity first)
az webapp identity assign \
  --name snappers-repair-shop \
  --resource-group snappers-rg

# Get the principal ID and grant access
PRINCIPAL_ID=$(az webapp identity show \
  --name snappers-repair-shop \
  --resource-group snappers-rg \
  --query principalId \
  --output tsv)

az keyvault set-policy \
  --name snappers-keyvault \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

## 📊 Monitoring & Logging

### Application Insights

Enable Application Insights for monitoring:

```bash
# Create Application Insights
az monitor app-insights component create \
  --app snappers-insights \
  --location eastus \
  --resource-group snappers-rg \
  --application-type web

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app snappers-insights \
  --resource-group snappers-rg \
  --query instrumentationKey \
  --output tsv)

# Configure app to use Application Insights
az webapp config appsettings set \
  --name snappers-repair-shop \
  --resource-group snappers-rg \
  --settings "ApplicationInsights__InstrumentationKey=$INSTRUMENTATION_KEY"
```

### Logging Levels

- **Development**: Information level for all components
- **Production**: Warning level for most components, Information for Application Insights

## 🛠️ Troubleshooting

### Database Connection Issues

```bash
# Test connection from local machine
dotnet ef database update --connection "YOUR_CONNECTION_STRING"

# Check firewall rules
az sql server firewall-rule list \
  --resource-group snappers-rg \
  --server snappers-sql-server
```

### Blob Storage Issues

```bash
# Verify container exists
az storage container show \
  --name job-photos \
  --account-name snappersstorage

# Check access permissions
az storage container show-permission \
  --name job-photos \
  --account-name snappersstorage
```

### Application Not Starting

1. Check Application Insights logs
2. Enable detailed error messages in Azure Portal (App Service > Configuration > General Settings > Detailed Error Messages)
3. Review deployment logs in Azure Portal

## 📝 Development Notes

### Photo Compression

Photos are automatically compressed on the client-side before upload:
- Maximum size: 2MB
- Maximum dimensions: 1920px (width or height)
- Format: JPEG with quality adjustment
- Implementation: `wwwroot/js/imageCompression.js`

### Input Validation

All models include comprehensive validation attributes:
- Required fields
- String length limits
- Email/Phone format validation
- Range validation for numeric fields

### Error Handling

- Global error boundary component: `Shared/ErrorBoundary.razor`
- ILogger integration throughout the application
- Polly retry policies for Azure Blob Storage operations

## 📄 License

Copyright © 2024 Snappers Repair Shop. All rights reserved.

## 🤝 Support

For issues or questions, please contact the development team.

---

**Built with ❤️ for Snappers Repair Shop, Fountain City, WI**

