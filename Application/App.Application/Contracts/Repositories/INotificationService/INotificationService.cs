
namespace App.Application.Contracts.Repositories.INotificationService
{ }
public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> MarkAsReadAsync(int notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<NotificationDto> GetNotificationByIdAsync(int notificationId, string userId);
    Task<List<MeetingDto>> GetTodayMeetingsAsync(string userId);
    Task<List<MeetingDto>> GetUpcomingMeetingsAsync(string userId, int minutesBefore = 30);
}

