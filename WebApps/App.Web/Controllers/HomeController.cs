using App.Application.Contracts.Repositories;
using App.Helper.Constants;
using App.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace App.Web.Controllers;

[PermissionAuthorize]
public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;

    private readonly IMeetingService _meetingService;


    public HomeController(IServiceProvider serviceProvider, ILogger<HomeController> logger, IMeetingService meetingService) : base(serviceProvider)
    {
        _logger = logger;
        _meetingService = meetingService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        var userDeptId = User.FindFirstValue("deptId");


        var dashboardData = new DashboardViewModel
        {
            UserRole = userRole,
            UserDepartment = await GetUserDepartmentName(userDeptId)
        };

        // Get meetings based on user role
        List<MeetingDto> meetings;

        //_logger.LogInformation($"User Role: {userRole}, User ID: {userId}, Department ID: {userDeptId}");

        if (userRole == "Super Admin")
        {
            // SuperAdmin sees all meetings
            meetings = (await _meetingService.GetAllMeetingsAsync()).ToList();
            //_logger.LogInformation($"SuperAdmin - Total meetings: {meetings.Count}");
        }
        else if (userRole == Roles.Admin || userRole == Roles.OfficeManager || userRole == Roles.Leader)
        {
            // Admin/OfficeManager/Leader sees department meetings + their own meetings with other departments
            if (!string.IsNullOrEmpty(userDeptId) && int.TryParse(userDeptId, out int deptId))
            {
                var departmentMeetings = await _meetingService.GetMeetingsByDepartmentAsync(deptId);
                var userMeetings = await _meetingService.GetUserMeetingsAsync(userId);

                //_logger.LogInformation($"Department meetings: {departmentMeetings.Count}, User meetings: {userMeetings.Count}");

                // Log department meetings
                foreach (var meeting in departmentMeetings)
                {
                    //_logger.LogInformation($"Dept Meeting: {meeting.Title} (ID: {meeting.Id})");
                }

                // Log user meetings
                foreach (var meeting in userMeetings)
                {
                    //_logger.LogInformation($"User Meeting: {meeting.Title} (ID: {meeting.Id})");
                }

                // Combine and remove duplicates based on meeting ID
                meetings = departmentMeetings.Union(userMeetings, new MeetingDtoComparer()).ToList();
                //_logger.LogInformation($"Combined meetings after deduplication: {meetings.Count}");

                // Log final meetings
                foreach (var meeting in meetings)
                {
                    //_logger.LogInformation($"Final Meeting: {meeting.Title} (ID: {meeting.Id})");
                }
            }
            else
            {
                // Fallback to user meetings if department ID is not available
                meetings = (await _meetingService.GetUserMeetingsAsync(userId)).ToList();
                //_logger.LogInformation($"Fallback - User meetings: {meetings.Count}");
            }
        }
        else
        {
            // Regular user sees only their meetings
            meetings = (await _meetingService.GetUserMeetingsAsync(userId)).ToList();
            //_logger.LogInformation($"Regular user - User meetings: {meetings.Count}");
        }

        // Calculate statistics
        dashboardData.TotalMeetings = meetings.Count;
        dashboardData.TodayMeetings = meetings.Count(m => m.StartTime.Date == DateTime.Today);
        dashboardData.CompletedMeetings = meetings.Count(m => m.MeetingStatusId == 2);
        dashboardData.PendingMeetings = meetings.Count(m => m.MeetingStatusId == 1);

        // Get upcoming meetings (from now onwards, excluding completed and cancelled meetings)
        var now = DateTime.Now;
        //_logger.LogInformation($"Current time for filtering: {now:yyyy-MM-dd HH:mm:ss}");

        var allUpcomingMeetings = meetings
            .Where(m => m.StartTime >= now &&
                       m.MeetingStatusId != 2 && // Not completed
                       m.MeetingStatusId != 3)   // Not cancelled
            .OrderBy(m => m.StartTime)
            .ToList();

        //_logger.LogInformation($"All upcoming meetings (after filtering): {allUpcomingMeetings.Count}");
        foreach (var meeting in allUpcomingMeetings)
        {
            //_logger.LogInformation($"Meeting: {meeting.Title}, Start: {meeting.StartTime}, Status: {meeting.MeetingStatusId}, Date >= Today: {meeting.StartTime.Date >= DateTime.Today}");
        }

        // Debug: Show all meetings before filtering
        //_logger.LogInformation($"All meetings before filtering: {meetings.Count}");
        foreach (var meeting in meetings)
        {
            //_logger.LogInformation($"All Meeting: {meeting.Title}, Start: {meeting.StartTime}, Status: {meeting.MeetingStatusId}, Date >= Today: {meeting.StartTime.Date >= DateTime.Today}, Not Completed: {meeting.MeetingStatusId != 2}, Not Cancelled: {meeting.MeetingStatusId != 3}");
        }

        dashboardData.UpcomingMeetings = allUpcomingMeetings
            .Take(5)
            .Select(m => new UpcomingMeeting
            {
                Id = m.Id,
                Title = m.Title,
                StartTime = m.StartTime,
                EndTime = m.EndTime,
                Location = m.Location,
                Priority = m.Priority,
                OrganizerName = m.OrganizerName
            })
            .ToList();

        //_logger.LogInformation($"Final upcoming meetings count: {dashboardData.UpcomingMeetings.Count}");

        // Get additional statistics
        dashboardData.TotalUsers = (await _userService.GetAllUsers()).Count;
        dashboardData.TotalDepartments = (await _unitOfWork.Repository<Department>().GetAllAsync()).Count;

        // Get meeting statistics for charts
        await PopulateChartData(dashboardData, meetings);

        return View(dashboardData);
    }

    private async Task<string> GetUserDepartmentName(string deptId)
    {
        if (string.IsNullOrEmpty(deptId)) return "غير محدد";

        var departments = await _unitOfWork.Repository<Department>()
            .GetAsync(d => d.Id == int.Parse(deptId));

        var department = departments.FirstOrDefault();

        return department?.Name ?? "غير محدد";
    }

    private async Task PopulateChartData(DashboardViewModel dashboard, List<MeetingDto> meetings)
    {

        // Meeting stats by month (last 6 months)
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var monthlyStats = meetings
            .Where(m => m.StartTime >= sixMonthsAgo)
            .GroupBy(m => new { m.StartTime.Year, m.StartTime.Month })
            .Select(g => new MeetingStatsByMonth
            {
                Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Count = g.Count()
            })
            .OrderBy(x => x.Month)
            .ToList();

        dashboard.MeetingStatsByMonth = monthlyStats;

        // Meeting stats by priority - ensure all priorities are shown even with 0 count
        var allPriorities = new[] { "عالي", "متوسط", "منخفض" };

        var priorityStats = allPriorities.Select(priority => new MeetingStatsByPriority
        {
            Priority = priority,
            Count = meetings.Count(m => (m.Priority ?? "غير محدد") == priority),
            Color = GetPriorityColor(priority)
        }).ToList();

        dashboard.MeetingStatsByPriority = priorityStats;

        // Meeting stats by status - ensure all statuses are shown even with 0 count
        var allStatuses = new[] { "مجدول", "مكتمل", "ملغي" };

        var statusStats = allStatuses.Select(status => new MeetingStatsByStatus
        {
            Status = status,
            Count = meetings.Count(m => GetStatusName(m.MeetingStatusId) == status),
            Color = GetStatusColor(status)
        }).ToList();

        dashboard.MeetingStatsByStatus = statusStats;
    }

    private string GetPriorityColor(string priority)
    {
        return priority switch
        {
            "High" or "عالي" => "#dc3545",      // Red
            "Medium" or "متوسط" => "#ffc107",   // Yellow
            "Low" or "منخفض" => "#28a745",      // Green
            "غير محدد" => "#6c757d",            // Gray
            _ => "#6c757d"                      // Default Gray
        };
    }

    private string GetStatusName(int? statusId)
    {
        return statusId switch
        {
            1 => "مجدول",
            2 => "مكتمل",
            3 => "ملغي",
            _ => "غير محدد"
        };
    }

    private string GetStatusColor(string status)
    {
        return status switch
        {
            "مجدول" => "#17a2b8",    // Blue - Scheduled
            "مكتمل" => "#28a745",    // Green - Completed
            "ملغي" => "#dc3545",     // Red - Cancelled
            _ => "#6c757d"           // Gray - Default
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public override bool Equals(object obj)
    {
        return obj is HomeController controller &&
               EqualityComparer<IMeetingService>.Default.Equals(_meetingService, controller._meetingService);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_meetingService);
    }

    // Comparer to remove duplicate meetings based on ID
    private class MeetingDtoComparer : IEqualityComparer<MeetingDto>
    {
        public bool Equals(MeetingDto x, MeetingDto y)
        {
            if (x == null || y == null)
                return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(MeetingDto obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
