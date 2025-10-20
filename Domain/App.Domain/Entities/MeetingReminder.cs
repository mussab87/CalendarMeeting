using App.Domain.UserSecurity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{ }
public class MeetingReminder : EntityBase
{
    [Required]
    public int MeetingId { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    public DateTime ReminderTime { get; set; }

    [Required]
    [MaxLength(50)]
    public string ReminderType { get; set; } // Email, Push, InApp

    public bool IsSent { get; set; } = false;

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey("MeetingId")]
    public virtual Meeting Meeting { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}

