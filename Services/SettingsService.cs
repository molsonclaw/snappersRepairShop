using Microsoft.EntityFrameworkCore;
using SnappersRepairShop.Data;
using SnappersRepairShop.Shared.Models;

namespace SnappersRepairShop.Services;

/// <summary>
/// Service for managing application settings
/// Loads the singleton AppSettings row and provides access to configuration
/// </summary>
public class SettingsService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SettingsService> _logger;
    private AppSettings? _cachedSettings;
    private DateTime _lastLoaded = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    // Event to notify when settings change
    public event Action? OnSettingsChanged;

    public SettingsService(
        IServiceScopeFactory scopeFactory,
        ILogger<SettingsService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current application settings (cached for 5 minutes)
    /// </summary>
    public async Task<AppSettings> GetSettingsAsync()
    {
        // Return cached settings if still valid
        if (_cachedSettings != null && DateTime.UtcNow - _lastLoaded < _cacheExpiration)
        {
            return _cachedSettings;
        }

        // Load settings from database
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _cachedSettings = await context.AppSettings.FirstOrDefaultAsync(s => s.AppSettingsId == 1);

        if (_cachedSettings == null)
        {
            _logger.LogWarning("AppSettings singleton not found in database. Creating default settings.");
            
            // Create default settings if not found
            _cachedSettings = new AppSettings
            {
                AppSettingsId = 1,
                BillingEnabled = false,
                ShopName = "Snappers Repair Shop",
                ShopAddress = "Fountain City, WI",
                TaxRate = 0.055m,
                DefaultLaborRate = 95.00m,
                LastModified = DateTime.UtcNow
            };

            context.AppSettings.Add(_cachedSettings);
            await context.SaveChangesAsync();
        }

        _lastLoaded = DateTime.UtcNow;
        return _cachedSettings;
    }

    /// <summary>
    /// Gets the BillingEnabled flag (most commonly accessed setting)
    /// </summary>
    public async Task<bool> GetBillingEnabledAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.BillingEnabled;
    }

    /// <summary>
    /// Updates the application settings and invalidates cache
    /// </summary>
    public async Task UpdateSettingsAsync(AppSettings updatedSettings)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settings = await context.AppSettings.FirstOrDefaultAsync(s => s.AppSettingsId == 1);
        
        if (settings == null)
        {
            throw new InvalidOperationException("AppSettings singleton not found");
        }

        // Update properties
        settings.BillingEnabled = updatedSettings.BillingEnabled;
        settings.ShopName = updatedSettings.ShopName;
        settings.ShopAddress = updatedSettings.ShopAddress;
        settings.ShopPhone = updatedSettings.ShopPhone;
        settings.ShopEmail = updatedSettings.ShopEmail;
        settings.TaxRate = updatedSettings.TaxRate;
        settings.DefaultLaborRate = updatedSettings.DefaultLaborRate;
        settings.Notes = updatedSettings.Notes;
        settings.LastModified = DateTime.UtcNow;
        settings.LastModifiedByUserId = updatedSettings.LastModifiedByUserId;

        await context.SaveChangesAsync();

        // Invalidate cache and notify listeners
        _cachedSettings = null;
        _lastLoaded = DateTime.MinValue;
        OnSettingsChanged?.Invoke();

        _logger.LogInformation("Application settings updated. BillingEnabled: {BillingEnabled}", 
            settings.BillingEnabled);
    }

    /// <summary>
    /// Refreshes the cached settings from the database
    /// </summary>
    public async Task RefreshSettingsAsync()
    {
        _cachedSettings = null;
        _lastLoaded = DateTime.MinValue;
        await GetSettingsAsync();
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// Toggles the BillingEnabled flag
    /// </summary>
    public async Task ToggleBillingEnabledAsync(string? userId = null)
    {
        var settings = await GetSettingsAsync();
        settings.BillingEnabled = !settings.BillingEnabled;
        settings.LastModifiedByUserId = userId;
        await UpdateSettingsAsync(settings);
    }
}

