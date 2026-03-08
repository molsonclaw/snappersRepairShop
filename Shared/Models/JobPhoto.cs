using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnappersRepairShop.Shared.Models;

/// <summary>
/// Represents a photo attached to a work order, stored in Azure Blob Storage
/// </summary>
public class JobPhoto
{
    [Key]
    public int JobPhotoId { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    [Required(ErrorMessage = "File name is required")]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Blob name is required")]
    [StringLength(500)]
    public string BlobName { get; set; } = string.Empty; // Unique blob identifier in Azure Storage

    [StringLength(500)]
    public string? ThumbnailBlobName { get; set; } // Thumbnail blob identifier

    [StringLength(200)]
    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    [StringLength(500)]
    public string? Caption { get; set; }

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? UploadedByUserId { get; set; } // Identity UserId

    // Navigation properties
    [ForeignKey(nameof(WorkOrderId))]
    public virtual WorkOrder WorkOrder { get; set; } = null!;
}

