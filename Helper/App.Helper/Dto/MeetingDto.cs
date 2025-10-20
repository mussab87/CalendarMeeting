

using System.ComponentModel.DataAnnotations;

namespace App.Helper.Dto { }
public class MeetingDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Authority { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? LocationId { get; set; }
    public string Location { get; set; }
    public MeetingTypeEnum MeetingType { get; set; } = MeetingTypeEnum.Internal;
    public string OrganizerId { get; set; }
    public string OrganizerName { get; set; }
    public int? OrganizerDepartmentId { get; set; }
    public string RecurrenceRule { get; set; }
    public bool IsRecurring { get; set; }
    public string MeetingPoints { get; set; }

    public int MeetingStatusId { get; set; }
    public int? PriorityId { get; set; }
    public string Priority { get; set; }
    public string PriorityColor { get; set; }
    public string Status { get; set; }
    public List<ParticipantDto> Participants { get; set; }
    public List<AttachmentDto> Attachments { get; set; }
    public List<MeetingFinishNoteDto> MeetingFinishNotes { get; set; }
}

public class CreateMeetingDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Authority { get; set; }
    public string MeetingPoints { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? LocationId { get; set; }
    //public string Location { get; set; }

    public MeetingTypeEnum MeetingType { get; set; }
    public string RecurrenceRule { get; set; }
    public bool IsRecurring { get; set; }

    public int MeetingStatusId { get; set; }
    public int? PriorityId { get; set; }
    public List<string>? ParticipantIds { get; set; }

    public string CreatedById { get; set; }
}

public class UpdateMeetingDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Authority { get; set; }
    public string MeetingPoints { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? LocationId { get; set; }
    //public string Location { get; set; }

    public MeetingTypeEnum MeetingType { get; set; }
    public string RecurrenceRule { get; set; }
    public bool IsRecurring { get; set; }

    public int MeetingStatusId { get; set; }
    public int? PriorityId { get; set; }
    public List<string>? ParticipantIds { get; set; }
}

public class ParticipantDto
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public ResponseStatusEnum ResponseStatus { get; set; }
    public string DeclinedReason { get; set; }
    public bool IsOrganizer { get; set; }
    public DateTime? RespondedAt { get; set; }

    public bool IsAttended { get; set; } = false;
    public DateTime? AttendedAt { get; set; }
}

public class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public long? FileSize { get; set; }
    public string ContentType { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; }
}

public class MeetingFinishNoteDto
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string UserId { get; set; }
    public string CreatedBy { get; set; }
    public string NoteFinishContent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AttachmentDto> Attachments { get; set; }
}

public class RsvpDto
{
    public int ResponseStatus { get; set; } // Accepted, Declined, Tentative
}

public class NotificationDto
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int? MeetingId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string NotificationType { get; set; } // Invitation, Update, Reminder, Cancellation
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Meeting details for quick display
    public string MeetingTitle { get; set; }
    public DateTime? MeetingStartTime { get; set; }
    public DateTime? MeetingEndTime { get; set; }
    public string MeetingLocation { get; set; }
    
    // Participant response status from MeetingParticipants table
    public ResponseStatusEnum? ParticipantResponseStatus { get; set; }
    
    // Organizer status from MeetingParticipants table
    public bool IsOrganizer { get; set; }
}

public class MeetingResponseDto
{
    [Required(ErrorMessage = "Meeting ID is required")]
    public int MeetingId { get; set; }

    [Required(ErrorMessage = "Response is required")]
    public ResponseStatusEnum ResponseStatus { get; set; } // Accepted or Declined

    public string DeclinedReason { get; set; } // Required if ResponseStatus is Declined
}

