using App.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.Web.Controllers
{ }

[PermissionAuthorize]
public class NotificationsController : BaseController
{
    private readonly INotificationService _notificationService;
    private readonly IMeetingService _meetingService;

    public NotificationsController(IServiceProvider serviceProvider, INotificationService notificationService, IMeetingService meetingService) : base(serviceProvider)
    {
        _notificationService = notificationService;
        _meetingService = meetingService;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // GET: /Notifications/GetUnreadCount
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }

    // GET: /Notifications/GetNotifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications(bool unreadOnly = false)
    {
        var userId = GetCurrentUserId();
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
        return Json(new { success = true, notifications });
    }

    // POST: /Notifications/MarkAsRead
    [HttpPost]
    //[ValidateAntiForgeryToken]
    //[IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
    {
        if (request == null || request.NotificationId <= 0)
        {
            return Json(new { success = false, message = "معرف الإشعار غير صحيح" });
        }

        var userId = GetCurrentUserId();
        var result = await _notificationService.MarkAsReadAsync(request.NotificationId, userId);

        if (result)
            return Json(new { success = true, message = "تم تحديث الإشعار" });

        return Json(new { success = false, message = "فشل تحديث الإشعار" });
    }

    public class MarkAsReadRequest
    {
        public int NotificationId { get; set; }
    }

    // POST: /Notifications/MarkAllAsRead
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.MarkAllAsReadAsync(userId);

        if (result)
            return Json(new { success = true, message = "تم تحديث جميع الإشعارات" });

        return Json(new { success = false, message = "فشل تحديث الإشعارات" });
    }

    // GET: /Notifications/GetTodayMeetings
    [HttpGet]
    public async Task<IActionResult> GetTodayMeetings()
    {
        var userId = GetCurrentUserId();
        var meetings = await _notificationService.GetTodayMeetingsAsync(userId);
        return Json(new { success = true, meetings });
    }

    // GET: /Notifications/GetUpcomingMeetings
    [HttpGet]
    public async Task<IActionResult> GetUpcomingMeetings(int minutesBefore = 30)
    {
        var userId = GetCurrentUserId();
        var meetings = await _notificationService.GetUpcomingMeetingsAsync(userId, minutesBefore);
        return Json(new { success = true, meetings });
    }

    // POST: /Notifications/RespondToMeeting
    [HttpPost]
    public async Task<IActionResult> RespondToMeeting([FromBody] MeetingResponseDto response)
    {
        //System.Diagnostics.Debug.WriteLine($"RespondToMeeting called with: MeetingId={response?.MeetingId}, ResponseStatus={response?.ResponseStatus}, DeclinedReason={response?.DeclinedReason}");

        if (response == null)
        {
            //System.Diagnostics.Debug.WriteLine("Response is null");
            return Json(new { success = false, message = "البيانات المرسلة فارغة" });
        }

        if (!ModelState.IsValid)
        {
            //System.Diagnostics.Debug.WriteLine($"ModelState is invalid: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            return Json(new { success = false, message = "بيانات غير صحيحة" });
        }

        var userId = GetCurrentUserId();

        // Validate declined reason if status is Declined
        if (response.ResponseStatus == ResponseStatusEnum.Declined && string.IsNullOrWhiteSpace(response.DeclinedReason))
        {
            return Json(new { success = false, message = "يجب إدخال سبب الرفض" });
        }

        try
        {
            // Update meeting participant response
            var participant = await _meetingService.GetMeetingParticipantAsync(response.MeetingId, userId);

            if (participant == null)
            {
                return Json(new { success = false, message = "أنت لست مشاركاً في هذا الاجتماع" });
            }

            // Update response status
            var result = await _meetingService.UpdateParticipantResponseAsync(
                response.MeetingId,
                userId,
                response.ResponseStatus,
                response.DeclinedReason);

            if (result)
            {
                var statusMessage = response.ResponseStatus == ResponseStatusEnum.Accepted
                    ? "تم قبول الدعوة بنجاح"
                    : "تم رفض الدعوة";

                return Json(new { success = true, message = statusMessage });
            }

            return Json(new { success = false, message = "فشل تحديث الحالة" });
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Error responding to meeting: {ex.Message}");
            return Json(new { success = false, message = "حدث خطأ أثناء تحديث الحالة" });
        }
    }

    // GET: /Notifications/GetMeetingDetails/{id}
    [HttpGet]
    public async Task<IActionResult> GetMeetingDetails(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var meeting = await _meetingService.GetMeetingByIdAsync(id);

            if (meeting == null)
            {
                return Json(new { success = false, message = "الاجتماع غير موجود" });
            }

            // Find current user's participant status
            var currentUserParticipant = meeting.Participants?.FirstOrDefault(p => p.UserId == userId);
            var userResponseStatus = currentUserParticipant?.ResponseStatus ?? ResponseStatusEnum.Pending;
            var isParticipant = currentUserParticipant != null;
            var isOrganizer = currentUserParticipant?.IsOrganizer ?? false;

            return Json(new
            {
                success = true,
                meeting,
                userResponseStatus = (int)userResponseStatus,
                isParticipant,
                isOrganizer
            });
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Error getting meeting details: {ex.Message}");
            return Json(new { success = false, message = "حدث خطأ أثناء جلب تفاصيل الاجتماع" });
        }
    }
}