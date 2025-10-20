using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Repositories.MeetingService
{ }
public class MeetingService : IMeetingService
{
    private readonly AppDbContext _context;
    private readonly IGenericRepository<Meeting> _meetingRepository;
    private readonly IGenericRepository<MeetingParticipant> _participantRepository;

    public MeetingService(AppDbContext context, IGenericRepository<Meeting> meetingRepository, IGenericRepository<MeetingParticipant> participantRepository)
    {
        _context = context;
        _meetingRepository = meetingRepository;
        _participantRepository = participantRepository;
    }

    public async Task<MeetingDto> GetMeetingByIdAsync(int id)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Location)
            .Include(m => m.MeetingStatus)
            .Include(m => m.Priority)
            .Include(m => m.Organizer)
            .Include(m => m.Participants)
                .ThenInclude(p => p.User)
            .Include(m => m.Attachments)
                .ThenInclude(a => a.Uploader)
            .Include(m => m.MeetingFinishNotes)
                .ThenInclude(n => n.User)
            .Include(m => m.MeetingFinishNotes)
                .ThenInclude(n => n.Attachments)
                    .ThenInclude(a => a.Uploader)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
            return null;

        // Get organizer's department ID
        var organizerDepartmentId = await _context.UserRoles
            .Where(ur => ur.UserId == meeting.OrganizerId)
            .Select(ur => (int?)ur.DepartmentId)
            .FirstOrDefaultAsync();

        return MapToDto(meeting, organizerDepartmentId);
    }

    public async Task<IEnumerable<MeetingDto>> GetAllMeetingsAsync()
    {
        var meetings = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Location)
            .Include(m => m.MeetingStatus)
            .Include(m => m.Priority)
            .Include(m => m.Participants)
                .ThenInclude(p => p.User)
            .ToListAsync();

        // Get organizer department IDs separately
        var organizerIds = meetings.Select(m => m.OrganizerId).Distinct().ToList();
        var organizerDepartments = await _context.UserRoles
            .Where(ur => organizerIds.Contains(ur.UserId))
            .GroupBy(ur => ur.UserId)
            .Select(g => new { UserId = g.Key, DepartmentId = g.FirstOrDefault().DepartmentId })
            .ToDictionaryAsync(x => x.UserId, x => x.DepartmentId);

        return meetings.Select(m => MapToDto(m, organizerDepartments.ContainsKey(m.OrganizerId) ? organizerDepartments[m.OrganizerId] : (int?)null));
    }

    public async Task<IEnumerable<MeetingDto>> GetMeetingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var meetings = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Location)
            .Include(m => m.MeetingStatus)
            .Include(m => m.Priority)
            .Include(m => m.Participants)
                .ThenInclude(p => p.User)
            .Where(m => m.StartTime >= startDate && m.StartTime <= endDate)
            .OrderBy(m => m.StartTime)
            .ToListAsync();

        // Get organizer department IDs separately
        var organizerIds = meetings.Select(m => m.OrganizerId).Distinct().ToList();
        var organizerDepartments = await _context.UserRoles
            .Where(ur => organizerIds.Contains(ur.UserId))
            .GroupBy(ur => ur.UserId)
            .Select(g => new { UserId = g.Key, DepartmentId = g.FirstOrDefault().DepartmentId })
            .ToDictionaryAsync(x => x.UserId, x => x.DepartmentId);

        return meetings.Select(m => MapToDto(m, organizerDepartments.ContainsKey(m.OrganizerId) ? organizerDepartments[m.OrganizerId] : (int?)null));
    }

    public async Task<IEnumerable<MeetingDto>> GetUserMeetingsAsync(string userId)
    {
        var meetings = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Location)
            .Include(m => m.MeetingStatus)
            .Include(m => m.Priority)
            .Include(m => m.Participants)
                .ThenInclude(p => p.User)
            .Where(m => m.OrganizerId == userId || m.Participants.Any(p => p.UserId == userId))
            .OrderBy(m => m.StartTime)
            .ToListAsync();

        // Get organizer department IDs separately
        var organizerIds = meetings.Select(m => m.OrganizerId).Distinct().ToList();
        var organizerDepartments = await _context.UserRoles
            .Where(ur => organizerIds.Contains(ur.UserId))
            .GroupBy(ur => ur.UserId)
            .Select(g => new { UserId = g.Key, DepartmentId = g.FirstOrDefault().DepartmentId })
            .ToDictionaryAsync(x => x.UserId, x => x.DepartmentId);

        return meetings.Select(m => MapToDto(m, organizerDepartments.ContainsKey(m.OrganizerId) ? organizerDepartments[m.OrganizerId] : (int?)null));
    }

    public async Task<IEnumerable<MeetingDto>> GetTodayMeetingsAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var meetings = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Location)
            .Include(m => m.MeetingStatus)
            .Include(m => m.Priority)
            .Include(m => m.Participants)
                .ThenInclude(p => p.User)
            .Where(m => (m.OrganizerId == userId || m.Participants.Any(p => p.UserId == userId))
                && m.StartTime >= today && m.StartTime < tomorrow)
            .OrderBy(m => m.StartTime)
            .ToListAsync();

        // Get organizer department IDs separately
        var organizerIds = meetings.Select(m => m.OrganizerId).Distinct().ToList();
        var organizerDepartments = await _context.UserRoles
            .Where(ur => organizerIds.Contains(ur.UserId))
            .GroupBy(ur => ur.UserId)
            .Select(g => new { UserId = g.Key, DepartmentId = g.FirstOrDefault().DepartmentId })
            .ToDictionaryAsync(x => x.UserId, x => x.DepartmentId);

        return meetings.Select(m => MapToDto(m, organizerDepartments.ContainsKey(m.OrganizerId) ? organizerDepartments[m.OrganizerId] : (int?)null));
    }

    public async Task<MeetingDto> CreateMeetingAsync(CreateMeetingDto createDto, string organizerId)
    {
        System.Diagnostics.Debug.WriteLine($"=== CreateMeetingAsync START ===");
        System.Diagnostics.Debug.WriteLine($"Creating meeting: {createDto.Title} for organizer: {organizerId}");

        var meeting = new Meeting
        {
            Title = createDto.Title,
            Description = createDto.Description,
            Authority = createDto.Authority,
            MeetingPoints = createDto.MeetingPoints,
            StartTime = Convert.ToDateTime(createDto.StartTime),
            EndTime = Convert.ToDateTime(createDto.EndTime),
            LocationId = createDto.LocationId,
            MeetingType = createDto.MeetingType,
            OrganizerId = organizerId,
            PriorityId = createDto.PriorityId,
            RecurrenceRule = createDto.RecurrenceRule,
            IsRecurring = (bool)createDto.IsRecurring,
            MeetingStatusId = 1, //createDto.MeetingStatusId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedById = createDto.CreatedById,
            CreatedDate = DateTime.UtcNow
        };

        await _context.Meetings.AddAsync(meeting);
        await _context.SaveChangesAsync();
        System.Diagnostics.Debug.WriteLine($"Meeting created with ID: {meeting.Id}");

        // Add organizer as participant
        System.Diagnostics.Debug.WriteLine($"Adding organizer as participant: {organizerId}");
        var organizerParticipant = new MeetingParticipant
        {
            MeetingId = meeting.Id,
            UserId = organizerId,
            ResponseStatus = ResponseStatusEnum.Accepted,
            IsOrganizer = true,
            IsAttended = false,
            InvitedAt = DateTime.UtcNow,
            RespondedAt = DateTime.UtcNow,
            CreatedById = createDto.CreatedById,
            CreatedDate = DateTime.UtcNow
        };
        await _context.MeetingParticipants.AddAsync(organizerParticipant);

        // Add other participants
        if (createDto.ParticipantIds != null && createDto.ParticipantIds.Any())
        {
            var filteredParticipants = createDto.ParticipantIds.Where(p => p != organizerId).ToList();
            System.Diagnostics.Debug.WriteLine($"Adding {filteredParticipants.Count} other participants");

            foreach (var participantId in filteredParticipants)
            {
                System.Diagnostics.Debug.WriteLine($"  - Adding participant: {participantId}");
                var participant = new MeetingParticipant
                {
                    MeetingId = meeting.Id,
                    UserId = participantId,
                    ResponseStatus = ResponseStatusEnum.Pending,
                    IsOrganizer = false,
                    IsAttended = false,
                    InvitedAt = DateTime.UtcNow,
                    CreatedById = createDto.CreatedById,
                    CreatedDate = DateTime.UtcNow
                };
                await _context.MeetingParticipants.AddAsync(participant);
            }
        }

        await _context.SaveChangesAsync();
        System.Diagnostics.Debug.WriteLine($"Participants saved to database");

        // Create notifications for all participants including organizer
        var allParticipants = new[] { organizerId }.Concat(createDto.ParticipantIds ?? new List<string>()).Distinct().ToList();
        System.Diagnostics.Debug.WriteLine($"Creating notifications for {allParticipants.Count} participants");

        await CreateNotificationsForMeetingAsync(
            meeting.Id,
            "تم إنشاء اجتماع جديد",
            $"تم إنشاء اجتماع جديد: {meeting.Title}",
            "Invitation",
            allParticipants
        );

        // Save notifications to database
        await _context.SaveChangesAsync();
        System.Diagnostics.Debug.WriteLine($"Notifications saved to database");
        System.Diagnostics.Debug.WriteLine($"=== CreateMeetingAsync END ===");

        return await GetMeetingByIdAsync(meeting.Id);
    }

    public async Task<MeetingDto> UpdateMeetingAsync(int id, UpdateMeetingDto updateDto)
    {
        try
        {
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
                return null;

            // Update basic meeting fields with validation
            if (!string.IsNullOrWhiteSpace(updateDto.Title))
                meeting.Title = updateDto.Title;

            meeting.Description = updateDto.Description ?? meeting.Description;
            meeting.Authority = updateDto.Authority ?? meeting.Authority;
            meeting.MeetingPoints = updateDto.MeetingPoints ?? meeting.MeetingPoints;
            meeting.StartTime = updateDto.StartTime;
            meeting.EndTime = updateDto.EndTime;
            meeting.LocationId = updateDto.LocationId;
            meeting.MeetingType = updateDto.MeetingType;
            // Ensure MeetingStatusId is valid (default to 1 if not provided)
            meeting.MeetingStatusId = updateDto.MeetingStatusId > 0 ? updateDto.MeetingStatusId : 1;
            meeting.PriorityId = updateDto.PriorityId;
            meeting.RecurrenceRule = updateDto.RecurrenceRule ?? meeting.RecurrenceRule;
            meeting.IsRecurring = updateDto.IsRecurring;
            meeting.UpdatedAt = DateTime.UtcNow;

            _context.Meetings.Update(meeting);

            // Get current participants BEFORE making changes
            var currentParticipantIds = await _context.MeetingParticipants
                .Where(p => p.MeetingId == id)
                .Select(p => p.UserId)
                .ToListAsync();

            // Handle participant updates
            await UpdateMeetingParticipantsAsync(id, updateDto.ParticipantIds, meeting.OrganizerId);

            // Build new participant list from DTO (includes organizer + new participants)
            var newParticipantIds = new List<string> { meeting.OrganizerId };
            if (updateDto.ParticipantIds != null && updateDto.ParticipantIds.Any())
            {
                newParticipantIds.AddRange(updateDto.ParticipantIds.Where(id => !string.IsNullOrEmpty(id)));
            }
            newParticipantIds = newParticipantIds.Distinct().ToList();

            // Handle notifications: Remove old ones for removed participants, create new ones for current participants
            await UpdateNotificationsForMeetingAsync(
                id,
                meeting.Title,
                currentParticipantIds,
                newParticipantIds
            );

            // Save all changes together
            await _context.SaveChangesAsync();

            return await GetMeetingByIdAsync(id);
        }
        catch (Exception ex)
        {
            // Log the exception here if you have logging
            throw new Exception($"Error updating meeting: {ex.Message}", ex);
        }
    }

    private async Task UpdateMeetingParticipantsAsync(int meetingId, List<string> newParticipantIds, string organizerId)
    {
        try
        {
            // Get current participants (excluding the organizer)
            var currentParticipants = await _context.MeetingParticipants
                .Where(p => p.MeetingId == meetingId && !p.IsOrganizer)
                .ToListAsync();

            // Get current participant IDs (excluding organizer)
            var currentParticipantIds = currentParticipants.Select(p => p.UserId).ToList();

            // Handle new participant IDs (exclude null values and organizer)
            var validNewParticipantIds = newParticipantIds?
                .Where(id => !string.IsNullOrEmpty(id) && id != organizerId)
                .ToList() ?? new List<string>();

            // Find participants to remove (in current but not in new)
            var participantsToRemove = currentParticipants
                .Where(p => !validNewParticipantIds.Contains(p.UserId))
                .ToList();

            // Find participants to add (in new but not in current)
            var participantsToAdd = validNewParticipantIds
                .Where(id => !currentParticipantIds.Contains(id))
                .ToList();

            // Debug information - you can remove this after testing
            //var debugInfo = $"MeetingId: {meetingId}, Current: [{string.Join(", ", currentParticipantIds)}], New: [{string.Join(", ", validNewParticipantIds)}], ToRemove: [{string.Join(", ", participantsToRemove.Select(p => p.UserId))}], ToAdd: [{string.Join(", ", participantsToAdd)}]";

            // Remove participants that are no longer in the meeting
            if (participantsToRemove.Any())
            {
                _context.MeetingParticipants.RemoveRange(participantsToRemove);
            }

            // Add new participants
            if (participantsToAdd.Any())
            {
                var newParticipants = participantsToAdd.Select(participantId => new MeetingParticipant
                {
                    MeetingId = meetingId,
                    UserId = participantId,
                    ResponseStatus = ResponseStatusEnum.Pending,
                    IsOrganizer = false,
                    IsAttended = false,
                    InvitedAt = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow
                }).ToList();

                await _context.MeetingParticipants.AddRangeAsync(newParticipants);
            }

            // Log the debug info - you can remove this after testing
            //System.Diagnostics.Debug.WriteLine(debugInfo);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating meeting participants: {ex.Message}", ex);
        }
    }

    private async Task CreateNotificationsForMeetingAsync(
        int meetingId,
        string title,
        string message,
        string notificationType,
        List<string> userIds)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"CreateNotificationsForMeetingAsync - MeetingId: {meetingId}, Type: {notificationType}, Users: {userIds.Count}");

            var notifications = userIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .Select(userId => new Notification
                {
                    UserId = userId,
                    MeetingId = meetingId,
                    Title = title,
                    Message = message,
                    NotificationType = notificationType,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow
                }).ToList();

            System.Diagnostics.Debug.WriteLine($"  - Created {notifications.Count} notification objects");

            if (notifications.Any())
            {
                await _context.Notifications.AddRangeAsync(notifications);
                System.Diagnostics.Debug.WriteLine($"  - Notifications added to context (pending SaveChangesAsync)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  - WARNING: No notifications to add!");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the meeting creation/update
            System.Diagnostics.Debug.WriteLine($"ERROR creating notifications: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task UpdateNotificationsForMeetingAsync(
        int meetingId,
        string meetingTitle,
        List<string> oldParticipantIds,
        List<string> newParticipantIds)
    {
        try
        {
            // Find participants who were removed (in old but not in new)
            var removedParticipantIds = oldParticipantIds
                .Where(id => !newParticipantIds.Contains(id))
                .ToList();

            // Remove notifications for removed participants
            if (removedParticipantIds.Any())
            {
                var notificationsToRemove = await _context.Notifications
                    .Where(n => n.MeetingId == meetingId && removedParticipantIds.Contains(n.UserId))
                    .ToListAsync();

                if (notificationsToRemove.Any())
                {
                    _context.Notifications.RemoveRange(notificationsToRemove);
                    System.Diagnostics.Debug.WriteLine($"Marked {notificationsToRemove.Count} notifications for removal");
                }
            }

            // Get existing unread notifications for current participants
            var existingUnreadNotifications = await _context.Notifications
                .Where(n => n.MeetingId == meetingId &&
                           newParticipantIds.Contains(n.UserId) &&
                           !n.IsRead)
                .Select(n => n.UserId)
                .ToListAsync();

            // Only create notifications for participants who don't have unread notifications
            var participantsNeedingNotification = newParticipantIds
                .Where(id => !existingUnreadNotifications.Contains(id))
                .ToList();

            if (participantsNeedingNotification.Any())
            {
                await CreateNotificationsForMeetingAsync(
                    meetingId,
                    "تم تحديث الاجتماع",
                    $"تم تحديث اجتماع: {meetingTitle}",
                    "Update",
                    participantsNeedingNotification
                );
                System.Diagnostics.Debug.WriteLine($"Creating notifications for {participantsNeedingNotification.Count} participants (skipped {existingUnreadNotifications.Count} with unread notifications)");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the meeting update
            System.Diagnostics.Debug.WriteLine($"Error updating notifications: {ex.Message}");
        }
    }

    public async Task<bool> DeleteMeetingAsync(int id)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return false;

        _context.Meetings.Remove(meeting);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> InviteParticipantsAsync(int meetingId, List<string> userIds)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        if (meeting == null)
            return false;

        foreach (var userId in userIds)
        {
            var existingParticipant = await _context.MeetingParticipants
                .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId);

            if (existingParticipant == null)
            {
                var participant = new MeetingParticipant
                {
                    MeetingId = meetingId,
                    UserId = userId,
                    ResponseStatus = ResponseStatusEnum.Pending,
                    IsOrganizer = false,
                    InvitedAt = DateTime.UtcNow
                };
                await _context.MeetingParticipants.AddAsync(participant);
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RespondToInvitationAsync(int meetingId, string userId, int responseStatus)
    {
        var participant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId);

        if (participant == null)
            return false;

        participant.ResponseStatus = (ResponseStatusEnum)responseStatus;
        participant.RespondedAt = DateTime.UtcNow;

        _context.MeetingParticipants.Update(participant);
        await _context.SaveChangesAsync();
        return true;
    }

    private MeetingDto MapToDto(Meeting meeting, int? organizerDepartmentId = null)
    {
        return new MeetingDto
        {
            Id = meeting.Id,
            Title = meeting.Title,
            Description = meeting.Description,
            Authority = meeting.Authority,
            MeetingPoints = meeting.MeetingPoints,
            StartTime = meeting.StartTime,
            EndTime = meeting.EndTime,
            LocationId = meeting.LocationId,
            Location = meeting.Location?.Location,
            MeetingType = meeting.MeetingType,
            OrganizerId = meeting.OrganizerId,
            OrganizerName = meeting.Organizer?.Name,
            OrganizerDepartmentId = organizerDepartmentId,
            RecurrenceRule = meeting.RecurrenceRule,
            IsRecurring = meeting.IsRecurring,
            MeetingStatusId = meeting.MeetingStatusId,
            PriorityId = meeting.PriorityId,
            Priority = meeting.Priority?.Priority,
            PriorityColor = meeting.Priority?.PriorityColor,
            Status = meeting.MeetingStatus?.Status,
            Participants = meeting.Participants?.Select(p => new ParticipantDto
            {
                UserId = p.UserId,
                Name = p.User?.Name,
                UserName = p.User?.UserName,
                Email = p.User?.Email,
                ResponseStatus = p.ResponseStatus,
                DeclinedReason = p.DeclinedReason,
                IsOrganizer = p.IsOrganizer,
                RespondedAt = p.RespondedAt,
                IsAttended = p.IsAttended,
                AttendedAt = p.AttendedAt
            }).ToList(),
            Attachments = meeting.Attachments?.Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                FilePath = a.FilePath,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                UploadedAt = a.UploadedAt,
                UploadedBy = a.Uploader?.Name ?? a.UploadedBy
            }).ToList(),
            MeetingFinishNotes = meeting.MeetingFinishNotes?.Select(n => new MeetingFinishNoteDto
            {
                Id = n.Id,
                MeetingId = n.MeetingId,
                UserId = n.UserId,
                CreatedBy = n.User?.Name ?? "غير محدد",
                NoteFinishContent = n.NoteFinishContent,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,
                Attachments = meeting.Attachments?.Where(a => a.MeetingFinishNoteId == n.Id).Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType,
                    UploadedAt = a.UploadedAt,
                    UploadedBy = a.Uploader?.Name ?? a.UploadedBy
                }).ToList()
            }).ToList()
        };
    }

    public async Task<ParticipantDto> GetMeetingParticipantAsync(int meetingId, string userId)
    {
        try
        {
            var participant = await _context.MeetingParticipants
                .Include(p => p.User)
                .Where(p => p.MeetingId == meetingId && p.UserId == userId)
                .Select(p => new ParticipantDto
                {
                    UserId = p.UserId,
                    Name = p.User.Name,
                    UserName = p.User.UserName,
                    Email = p.User.Email,
                    ResponseStatus = p.ResponseStatus,
                    IsOrganizer = p.IsOrganizer,
                    IsAttended = p.IsAttended,
                    AttendedAt = p.AttendedAt,
                    DeclinedReason = p.DeclinedReason
                })
                .FirstOrDefaultAsync();

            return participant;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting participant: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateParticipantResponseAsync(int meetingId, string userId, ResponseStatusEnum responseStatus, string declinedReason)
    {
        try
        {
            var participant = await _context.MeetingParticipants
                .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId);

            if (participant == null)
                return false;

            participant.ResponseStatus = responseStatus;
            participant.RespondedAt = DateTime.UtcNow;

            // If declined, store the reason
            if (responseStatus == ResponseStatusEnum.Declined)
            {
                participant.DeclinedReason = declinedReason;
            }
            else
            {
                participant.DeclinedReason = null; // Clear reason if accepting
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating participant response: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<object>> GetAcceptedParticipantsAsync(int meetingId)
    {
        try
        {
            var participants = await _context.MeetingParticipants
                .Where(p => p.MeetingId == meetingId && p.ResponseStatus == ResponseStatusEnum.Accepted)
                .Join(_context.Users,
                    mp => mp.UserId,
                    u => u.Id,
                    (mp, u) => new
                    {
                        userId = mp.UserId,
                        userName = u.UserName,
                        name = u.Name,
                        isAttended = mp.IsAttended
                    })
                .ToListAsync();

            return participants;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting accepted participants: {ex.Message}");
            return new List<object>();
        }
    }

    public async Task<IEnumerable<MeetingDto>> GetMeetingsByDepartmentAsync(int departmentId)
    {
        try
        {
            // First get all user IDs in the department
            var userIdsInDepartment = await _context.UserRoles
                .Where(ur => ur.DepartmentId == departmentId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            // Then get meetings organized by users in this department
            var meetings = await _context.Meetings
                .Include(m => m.Organizer)
                .Include(m => m.Location)
                .Include(m => m.MeetingStatus)
                .Include(m => m.Priority)
                .Include(m => m.Participants)
                    .ThenInclude(mp => mp.User)
                .Include(m => m.Attachments)
                .Include(m => m.MeetingFinishNotes)
                    .ThenInclude(mfn => mfn.Attachments)
                .Where(m => userIdsInDepartment.Contains(m.OrganizerId))
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            // Get organizer department IDs in batch for performance
            var organizerIds = meetings.Select(m => m.OrganizerId).Distinct().ToList();
            var organizerDeptIds = await _context.UserRoles
                .Where(ur => organizerIds.Contains(ur.UserId))
                .Select(ur => new { ur.UserId, ur.DepartmentId })
                .ToDictionaryAsync(x => x.UserId, x => x.DepartmentId);

            return meetings.Select(m => MapToDto(m, organizerDeptIds.GetValueOrDefault(m.OrganizerId)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting meetings by department: {ex.Message}");
            return new List<MeetingDto>();
        }
    }
}

