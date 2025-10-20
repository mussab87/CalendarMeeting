using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Repositories.NotificationService
{ }
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        try
        {
            var query = _context.Notifications
                .Include(n => n.Meeting)
                    .ThenInclude(m => m.Location)
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    MeetingId = n.MeetingId,
                    Title = n.Title,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt,
                    MeetingTitle = n.Meeting != null ? n.Meeting.Title : null,
                    MeetingStartTime = n.Meeting != null ? n.Meeting.StartTime : null,
                    MeetingEndTime = n.Meeting != null ? n.Meeting.EndTime : null,
                    MeetingLocation = n.Meeting != null && n.Meeting.Location != null ? n.Meeting.Location.Location : null,
                    // Get participant response status from MeetingParticipants table
                    ParticipantResponseStatus = n.MeetingId != null 
                        ? _context.MeetingParticipants
                            .Where(mp => mp.MeetingId == n.MeetingId && mp.UserId == userId)
                            .Select(mp => (ResponseStatusEnum?)mp.ResponseStatus)
                            .FirstOrDefault()
                        : null,
                    // Get organizer status from MeetingParticipants table
                    IsOrganizer = n.MeetingId != null 
                        ? _context.MeetingParticipants
                            .Where(mp => mp.MeetingId == n.MeetingId && mp.UserId == userId)
                            .Select(mp => mp.IsOrganizer)
                            .FirstOrDefault()
                        : false
                })
                .ToListAsync();

            return notifications;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting notifications: {ex.Message}");
            return new List<NotificationDto>();
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        try
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting unread count: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking notification as read: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"MarkAllAsReadAsync - UserId: {userId}");
            
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"Found {unreadNotifications.Count} unread notifications for user {userId}");

            if (!unreadNotifications.Any())
            {
                System.Diagnostics.Debug.WriteLine("No unread notifications found - returning true");
                return true;
            }

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                System.Diagnostics.Debug.WriteLine($"Marked notification {notification.Id} as read");
            }

            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"Successfully marked {unreadNotifications.Count} notifications as read");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR marking all notifications as read: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<NotificationDto> GetNotificationByIdAsync(int notificationId, string userId)
    {
        try
        {
            var notification = await _context.Notifications
                .Include(n => n.Meeting)
                    .ThenInclude(m => m.Location)
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    MeetingId = n.MeetingId,
                    Title = n.Title,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt,
                    MeetingTitle = n.Meeting != null ? n.Meeting.Title : null,
                    MeetingStartTime = n.Meeting != null ? n.Meeting.StartTime : null,
                    MeetingEndTime = n.Meeting != null ? n.Meeting.EndTime : null,
                    MeetingLocation = n.Meeting != null && n.Meeting.Location != null ? n.Meeting.Location.Location : null,
                    // Get participant response status from MeetingParticipants table
                    ParticipantResponseStatus = n.MeetingId != null 
                        ? _context.MeetingParticipants
                            .Where(mp => mp.MeetingId == n.MeetingId && mp.UserId == userId)
                            .Select(mp => (ResponseStatusEnum?)mp.ResponseStatus)
                            .FirstOrDefault()
                        : null,
                    // Get organizer status from MeetingParticipants table
                    IsOrganizer = n.MeetingId != null 
                        ? _context.MeetingParticipants
                            .Where(mp => mp.MeetingId == n.MeetingId && mp.UserId == userId)
                            .Select(mp => mp.IsOrganizer)
                            .FirstOrDefault()
                        : false
                })
                .FirstOrDefaultAsync();

            return notification;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting notification by ID: {ex.Message}");
            return null;
        }
    }

    public async Task<List<MeetingDto>> GetTodayMeetingsAsync(string userId)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var meetings = await _context.MeetingParticipants
                .Include(mp => mp.Meeting)
                    .ThenInclude(m => m.Location)
                .Include(mp => mp.Meeting.Organizer)
                .Where(mp => mp.UserId == userId &&
                            mp.Meeting.StartTime >= today &&
                            mp.Meeting.StartTime < tomorrow)
                .Select(mp => new MeetingDto
                {
                    Id = mp.Meeting.Id,
                    Title = mp.Meeting.Title,
                    Description = mp.Meeting.Description,
                    StartTime = mp.Meeting.StartTime,
                    EndTime = mp.Meeting.EndTime,
                    Location = mp.Meeting.Location != null ? mp.Meeting.Location.Location : null,
                    OrganizerName = mp.Meeting.Organizer != null ? mp.Meeting.Organizer.Name : null
                })
                .ToListAsync();

            return meetings;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting today's meetings: {ex.Message}");
            return new List<MeetingDto>();
        }
    }

    public async Task<List<MeetingDto>> GetUpcomingMeetingsAsync(string userId, int minutesBefore = 30)
    {
        try
        {
            var now = DateTime.UtcNow;
            var upcomingTime = now.AddMinutes(minutesBefore);

            var meetings = await _context.MeetingParticipants
                .Include(mp => mp.Meeting)
                    .ThenInclude(m => m.Location)
                .Include(mp => mp.Meeting.Organizer)
                .Where(mp => mp.UserId == userId &&
                            mp.Meeting.StartTime > now &&
                            mp.Meeting.StartTime <= upcomingTime)
                .Select(mp => new MeetingDto
                {
                    Id = mp.Meeting.Id,
                    Title = mp.Meeting.Title,
                    Description = mp.Meeting.Description,
                    StartTime = mp.Meeting.StartTime,
                    EndTime = mp.Meeting.EndTime,
                    Location = mp.Meeting.Location != null ? mp.Meeting.Location.Location : null,
                    OrganizerName = mp.Meeting.Organizer != null ? mp.Meeting.Organizer.Name : null
                })
                .ToListAsync();

            return meetings;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting upcoming meetings: {ex.Message}");
            return new List<MeetingDto>();
        }
    }
}

