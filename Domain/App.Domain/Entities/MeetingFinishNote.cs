using App.Domain.UserSecurity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{ }
/// <summary>
/// ����� ��������
/// </summary>
public class MeetingFinishNote : EntityBase
{
    [Required]
    public int MeetingId { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    public string NoteFinishContent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey("MeetingId")]
    public virtual Meeting Meeting { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public virtual ICollection<MeetingAttachment> Attachments { get; set; }
}

