using App.Domain.UserSecurity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{ }
public class MeetingAttachment : EntityBase
{
    [Required]
    public int MeetingId { get; set; }

    public int? MeetingFinishNoteId { get; set; }

    [Required]
    [MaxLength(300)]
    public string FileName { get; set; }

    [Required]
    [MaxLength(1000)]
    public string FilePath { get; set; }

    public long? FileSize { get; set; }

    [MaxLength(200)]
    public string ContentType { get; set; }

    [Required]
    public string UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey("MeetingId")]
    public virtual Meeting Meeting { get; set; }

    [ForeignKey("MeetingFinishNoteId")]
    public virtual MeetingFinishNote MeetingFinishNote { get; set; }

    [ForeignKey("UploadedBy")]
    public virtual User Uploader { get; set; }
}

