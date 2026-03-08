using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnappersRepairShop.Shared.Models;

/// <summary>
/// Represents a vehicle owned by a customer
/// </summary>
public class Vehicle
{
    [Key]
    public int VehicleId { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Year is required")]
    [Range(1900, 2100, ErrorMessage = "Please enter a valid year")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Make is required")]
    [StringLength(100)]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required")]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Color { get; set; }

    [Required(ErrorMessage = "VIN is required")]
    [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN must be exactly 17 characters")]
    public string VIN { get; set; } = string.Empty;

    [StringLength(20)]
    public string? LicensePlate { get; set; }

    [StringLength(50)]
    public string? EngineType { get; set; }

    public int? Mileage { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    // Computed property for display
    public string DisplayName => $"{Year} {Make} {Model}";
}

