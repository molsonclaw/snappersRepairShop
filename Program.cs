using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SnappersRepairShop.Data;
using SnappersRepairShop.Services;
using SnappersRepairShop.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Add detailed error logging for Blazor circuits
    options.DetailedErrors = true;
});
builder.Services.AddSignalR(options =>
{
    // Add detailed error logging for SignalR
    options.EnableDetailedErrors = true;
});

// Configure Entity Framework Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add database developer page filter for detailed error pages in development
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>() // Enable role management
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Register custom services
builder.Services.AddScoped<BlobStorageService>();
builder.Services.AddSingleton<SettingsService>(); // Singleton to cache settings across requests
builder.Services.AddSingleton<LicensePlateOcrService>();

// Add authorization policies for roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TechnicianOrAbove", policy => policy.RequireRole("Admin", "Technician"));
    options.AddPolicy("OfficeOrAbove", policy => policy.RequireRole("Admin", "Office", "Technician"));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
// Temporarily disable WorkOrderHub to fix circuit breaking issue
// app.MapHub<WorkOrderHub>("/workorderhub"); // Map SignalR hub
app.MapFallbackToPage("/_Host");

// Initialize blob storage and apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Apply pending migrations (this will seed roles and default admin user)
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        // Initialize blob storage (skip if connection string is not configured)
        var config = services.GetRequiredService<IConfiguration>();
        var blobConnectionString = config["AzureBlobStorage:ConnectionString"];

        if (!string.IsNullOrEmpty(blobConnectionString) &&
            !blobConnectionString.Contains("YOUR_ACCOUNT") &&
            blobConnectionString != "UseDevelopmentStorage=true")
        {
            var blobService = services.GetRequiredService<BlobStorageService>();
            await blobService.InitializeAsync();
            app.Logger.LogInformation("Blob storage initialized successfully");
        }
        else
        {
            app.Logger.LogWarning("Blob storage not configured - photo uploads will be disabled");
        }

        // Seed sample data
        await DbSeeder.SeedSampleData(context);
        app.Logger.LogInformation("Sample data seeded successfully");

        app.Logger.LogInformation("Database initialized successfully. Default admin: admin@snappersrepair.com / Admin@123");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

app.Run();

