using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnappersRepairShop.Shared.Models;

/// <summary>
/// Represents a part used on a work order
/// </summary>
public class PartUsed
{
    [Key]
    public int PartUsedId { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    [Required(ErrorMessage = "Part number is required")]
    [StringLength(100)]
    public string PartNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 9999, ErrorMessage = "Quantity must be between 1 and 9999")]
    public int Quantity { get; set; }

    // Hidden when BillingEnabled = false
    [Required(ErrorMessage = "Cost is required")]
    [Range(0, 999999.99, ErrorMessage = "Cost must be between 0 and 999999.99")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [StringLength(200)]
    public string? Supplier { get; set; }

    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(WorkOrderId))]
    public virtual WorkOrder WorkOrder { get; set; } = null!;

    /// <summary>
    /// Calculates the total based on quantity and unit cost
    /// </summary>
    public void CalculateTotal()
    {
        Total = Quantity * UnitCost;
    }
}

