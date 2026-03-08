using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnappersRepairShop.Shared.Models;

/// <summary>
/// Global application settings - singleton table with one row
/// Controls feature visibility and app-wide configuration
/// </summary>
public class AppSettings
{
    [Key]
    public int AppSettingsId { get; set; } // Always 1 - singleton pattern

    /// <summary>
    /// Master toggle for all billing/pricing features
    /// When false, ALL price/cost fields are hidden throughout the application
    /// </summary>
    public bool BillingEnabled { get; set; } = false;

    [StringLength(200)]
    public string? ShopName { get; set; } = "Snappers Repair Shop";

    [StringLength(500)]
    public string? ShopAddress { get; set; }

    [StringLength(20)]
    public string? ShopPhone { get; set; }

    [StringLength(200)]
    public string? ShopEmail { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal TaxRate { get; set; } = 0.055m; // 5.5% default for Wisconsin

    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultLaborRate { get; set; } = 95.00m;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? LastModifiedByUserId { get; set; }
}

