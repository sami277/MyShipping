using Microsoft.AspNetCore.Identity;

namespace MyShipping.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
