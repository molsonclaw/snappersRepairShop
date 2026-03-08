using System.ComponentModel.DataAnnotations;

namespace SnappersRepairShop.Shared.Models;

/// <summary>
/// Represents a customer of the repair shop
/// </summary>
public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(2)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? ZipCode { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation properties
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    // Computed property for display
    public string FullName => $"{FirstName} {LastName}";
}

