using App.Domain.UserSecurity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{ }
public class MeetingParticipant : EntityBase
{
    [Required]
    public int MeetingId { get; set; }

    [Required]
    public string UserId { get; set; }

    //[MaxLength(50)]
    //public string ResponseStatus { get; set; } = "Pending"; // Pending, Accepted, Declined, Tentative
    public ResponseStatusEnum ResponseStatus { get; set; } = ResponseStatusEnum.Pending;
    [MaxLength(500)]
    public string DeclinedReason { get; set; }

    public bool IsOrganizer { get; set; } = false;

    public bool IsAttended { get; set; } = false;
    public DateTime? AttendedAt { get; set; }

    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    // Navigation Properties
    [ForeignKey("MeetingId")]
    public virtual Meeting Meeting { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}
