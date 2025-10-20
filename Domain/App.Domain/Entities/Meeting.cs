using App.Domain.UserSecurity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{ }
public class Meeting : EntityBase
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; }

    //جهة الاجتماع
    [MaxLength(300)]
    public string Authority { get; set; }
    //اجندة الاجتماع    
    public string MeetingPoints { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    //[MaxLength(500)]
    //public string Location { get; set; }
    public int? LocationId { get; set; }
    [ForeignKey("LocationId")]
    public virtual MeetingLocation Location { get; set; }

    public int? PriorityId { get; set; }
    [ForeignKey("PriorityId")]
    public virtual MeetingPriority Priority { get; set; }

    public MeetingTypeEnum MeetingType { get; set; } = MeetingTypeEnum.Internal;

    [Required]
    public string OrganizerId { get; set; }
    // Navigation Properties
    [ForeignKey("OrganizerId")]
    public virtual User Organizer { get; set; }


    [MaxLength(500)]
    public string RecurrenceRule { get; set; } // iCal RRULE format

    public bool IsRecurring { get; set; } = false;

    //[MaxLength(50)]
    //public string Status { get; set; } = "Scheduled"; // Scheduled, Cancelled, Completed
    public int MeetingStatusId { get; set; }

    [ForeignKey("MeetingStatusId")]
    public virtual MeetingStatus MeetingStatus { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


    public virtual ICollection<MeetingParticipant> Participants { get; set; }
    public virtual ICollection<MeetingAttachment> Attachments { get; set; }
    public virtual ICollection<MeetingReminder> Reminders { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; }
    //public virtual ICollection<MeetingNote> Notes { get; set; }
    public virtual ICollection<MeetingFinishNote> MeetingFinishNotes { get; set; }
}

