using App.Domain.UserSecurity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{ }
public class Notification : EntityBase
{
    [Required]
    public string UserId { get; set; }

    public int? MeetingId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; }

    [Required]
    public string Message { get; set; }

    [Required]
    [MaxLength(50)]
    public string NotificationType { get; set; } // Invitation, Reminder, Update, Cancellation

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    [ForeignKey("MeetingId")]
    public virtual Meeting Meeting { get; set; }
}

