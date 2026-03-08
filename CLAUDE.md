# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Snappers Repair Shop is a Blazor Server application (.NET 9) for managing an auto repair shop. It uses MudBlazor for UI, Entity Framework Core with SQL Server, ASP.NET Core Identity for auth, and Azure Blob Storage for photo management.

## Build & Run Commands

```bash
dotnet restore              # Restore dependencies
dotnet build                # Build the project
dotnet run                  # Run (HTTPS: localhost:5001, HTTP: localhost:5002)
dotnet ef database update   # Apply migrations (auto-runs on startup too)
dotnet ef migrations add <Name>  # Create a new migration
dotnet publish -c Release -o ./publish  # Production publish
```

Solution file: `Snappers.sln`, project file: `SnappersRepairShop.csproj`.

No test project exists currently.

## Architecture

### Data Flow
Pages inject `ApplicationDbContext` directly for data access (no repository abstraction). Two services abstract infrastructure concerns:
- **BlobStorageService** (scoped) - Azure Blob Storage photo upload/retrieval with SAS URLs, thumbnail generation via ImageSharp, Polly retry policies
- **SettingsService** (singleton) - Caches `AppSettings` row with 5-minute TTL, exposes `OnSettingsChanged` event

### Cascading State
`AppSettingsProvider.razor` wraps the app and cascades `BillingEnabled` (bool) and full `AppSettings` to all components. Pages check these cascading parameters to conditionally render billing/pricing columns.

### Key Data Model
- **Customer** -> has many **Vehicle** (restrict delete)
- **WorkOrder** -> belongs to Customer + Vehicle, has many **LaborLine**, **PartUsed**, **JobPhoto** (cascade delete)
- **AppSettings** - singleton row controlling billing toggle, shop info, default labor rate ($95/hr), tax rate (5.5%)

Models live in `Shared/Models/`. The DbContext is `Data/ApplicationDbContext.cs` with seeding logic. `Data/DbSeeder.cs` seeds sample data on startup.

### Authentication & Roles
ASP.NET Core Identity with three roles: **Admin**, **Technician**, **Office**.
- Admin: full access including Settings page and user management
- Technician: only sees own assigned work orders, **never** sees billing fields
- Office: can view all work orders but cannot create/edit them or access settings

Authorization policies defined in `Program.cs`: `AdminOnly`, `TechnicianOrAbove`, `OfficeOrAbove`.

### BillingEnabled Toggle
Global feature flag stored in `AppSettings`. When toggled ON via Settings page, it backfills calculated totals on existing work orders. Technicians never see billing fields regardless of this setting.

### SignalR Hub
`Hubs/WorkOrderHub.cs` exists for real-time updates but is **currently disabled** in `Program.cs` (commented out) due to circuit-breaking issues.

### Pages (Routes)
| Route | File | Purpose |
|---|---|---|
| `/` | `Pages/Index.razor` | Dashboard with KPI cards |
| `/workorders` | `Pages/WorkOrders.razor` | Filterable work order list |
| `/workorders/{id}` | `Pages/WorkOrderDetails.razor` | Work order detail view |
| `/jobs/new` | `Pages/JobForm.razor` | Create/edit work order |
| `/settings` | `Pages/Settings.razor` | Admin-only app config |

### Client-Side
- `wwwroot/css/site.css` - Mobile-first responsive CSS with tablet/shop-bay optimizations
- `wwwroot/js/imageCompression.js` - Client-side image compression before upload (max 2MB, 1920px)

## Configuration

Connection strings and blob storage config in `appsettings.{Environment}.json`. Dev uses LocalDB and `UseDevelopmentStorage=true` (Azurite). Blob storage is optional for dev - photo uploads are disabled if not configured.

Default admin credentials (seeded): `admin@snappersrepair.com` / `Admin@123`

## Key Conventions

- Blazor components use code-behind files (`.razor.cs`) only for `App.razor`; all other pages use `@code` blocks inline
- Data loading happens in `OnInitializedAsync()`; UI updates via `StateHasChanged()`
- Photo storage uses private blob containers with time-limited SAS URLs (1 hour expiry)
- File validation: 10MB max, allowed extensions: .jpg, .jpeg, .png, .gif, .bmp
- Thumbnail generation: 300x300px JPEG at 85% quality
- Foreign keys use `DeleteBehavior.Restrict` for Customer/Vehicle, `Cascade` for child line items
