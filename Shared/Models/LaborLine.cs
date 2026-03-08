using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnappersRepairShop.Shared.Models;

/// <summary>
/// Represents a labor line item on a work order
/// </summary>
public class LaborLine
{
    [Key]
    public int LaborLineId { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hours are required")]
    [Range(0.1, 999.9, ErrorMessage = "Hours must be between 0.1 and 999.9")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Hours { get; set; }

    // Hidden when BillingEnabled = false
    [Required(ErrorMessage = "Rate is required")]
    [Range(0, 9999.99, ErrorMessage = "Rate must be between 0 and 9999.99")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RatePerHour { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [StringLength(100)]
    public string? TechnicianId { get; set; } // Identity UserId

    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(WorkOrderId))]
    public virtual WorkOrder WorkOrder { get; set; } = null!;

    /// <summary>
    /// Calculates the total based on hours and rate
    /// </summary>
    public void CalculateTotal()
    {
        Total = Hours * RatePerHour;
    }
}

