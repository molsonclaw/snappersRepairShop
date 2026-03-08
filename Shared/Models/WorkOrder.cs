using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnappersRepairShop.Shared.Models;

/// <summary>
/// Represents a work order/repair job
/// </summary>
public class WorkOrder
{
    [Key]
    public int WorkOrderId { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Work order number is required")]
    [StringLength(50)]
    public string WorkOrderNumber { get; set; } = string.Empty;

    [Required]
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public DateTime? DateStarted { get; set; }

    public DateTime? DateCompleted { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed, Cancelled

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? DiagnosisNotes { get; set; }

    [StringLength(2000)]
    public string? CompletionNotes { get; set; }

    public int? CurrentMileage { get; set; }

    [StringLength(100)]
    public string? AssignedTechnicianId { get; set; } // Identity UserId

    // Billing fields - hidden when BillingEnabled = false
    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PartsTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrandTotal { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? DatePaid { get; set; }

    public DateTime? ModifiedDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(VehicleId))]
    public virtual Vehicle Vehicle { get; set; } = null!;

    public virtual ICollection<LaborLine> LaborLines { get; set; } = new List<LaborLine>();
    public virtual ICollection<PartUsed> PartsUsed { get; set; } = new List<PartUsed>();
    public virtual ICollection<JobPhoto> JobPhotos { get; set; } = new List<JobPhoto>();
}

