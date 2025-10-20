using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace App.Domain.UserSecurity
{
    public class User : IdentityUser<string>
    {
        [MaxLength(400)]
        public string Name { get; set; }
        public override string UserName { get => base.UserName; set => base.UserName = value; }
        public bool? FirstLogin { get; set; }
        public bool? MaxMonthLock { get; set; }
        public bool? MonthLockStatus { get; set; }
        public bool? UserStatus { get; set; }
        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; }

        [MaxLength(100)]
        public string Timezone { get; set; } = "Asia/Riyadh";

        public string NotificationPreferences { get; set; } // JSON

        //public DateTime? LastPasswordChangedDate { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Meeting> OrganizedMeetings { get; set; }
        public virtual ICollection<MeetingParticipant> MeetingParticipants { get; set; }
        public virtual ICollection<MeetingAttachment> UploadedAttachments { get; set; }
        public virtual ICollection<MeetingReminder> Reminders { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        //public virtual ICollection<MeetingNote> Notes { get; set; }
        public virtual ICollection<MeetingFinishNote> MeetingFinishNotes { get; set; }
    }
}

