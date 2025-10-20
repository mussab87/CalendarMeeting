using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using App.Helper.Constants;

namespace App.Web.Controllers
{
    [PermissionAuthorize]
    //[AllowAnonymous]
    public class MeetingsController : BaseController
    {
        private readonly IMeetingService _meetingService;

        public MeetingsController(IServiceProvider serviceProvider, IMeetingService meetingService) : base(serviceProvider)
        {
            _meetingService = meetingService;
        }



        // GET: api/meetings
        [HttpGet("api/meetings")]
        public async Task<IActionResult> GetMeetings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userDeptId = User.FindFirstValue("deptId");

            // Get meetings based on user role
            IEnumerable<MeetingDto> meetings;
            if (userRole == Roles.SuperAdmin)
            {
                // SuperAdmin sees all meetings
                meetings = await this._meetingService.GetAllMeetingsAsync();
            }
            else if (userRole == Roles.Admin || userRole == Roles.OfficeManager || userRole == Roles.Leader)
            {
                // Admin/OfficeManager/Leader sees department meetings + their own meetings with other departments
                if (!string.IsNullOrEmpty(userDeptId) && int.TryParse(userDeptId, out int deptId))
                {
                    var departmentMeetings = await this._meetingService.GetMeetingsByDepartmentAsync(deptId);
                    var userMeetings = await this._meetingService.GetUserMeetingsAsync(userId);
                    
                    // Combine and remove duplicates based on meeting ID
                    meetings = departmentMeetings.Union(userMeetings, new MeetingDtoComparer()).ToList();
                }
                else
                {
                    // Fallback to user meetings if department ID is not available
                    meetings = await this._meetingService.GetUserMeetingsAsync(userId);
                }
            }
            else
            {
                // Regular user sees only their meetings
                meetings = await this._meetingService.GetUserMeetingsAsync(userId);
            }
            var events = meetings.Select(m => new
            {
                id = m.Id,
                title = m.Title,
                start = m.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = m.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                description = m.Description,
                location = m.Location,
                status = m.Status,
                // Enhanced display information
                displayTitle = $"{m.StartTime:HH:mm} - {m.Title}", // Time + Title for better display
                extendedProps = new
                {
                    description = m.Description,
                    location = m.Location,
                    organizer = m.OrganizerName ?? "غير محدد",
                    priority = m.Priority ?? "عادي",
                    priorityColor = m.PriorityColor ?? "#007bff",
                    meetingType = m.MeetingType == MeetingTypeEnum.Internal ? "داخلي" : "خارجي",
                    fullDetails = $"الاجتماع: {m.Title}\nالوقت: {m.StartTime:HH:mm} - {m.EndTime:HH:mm}\nالموقع: {m.Location ?? "غير محدد"}\nالمنظم: {m.OrganizerName ?? "غير محدد"}\nالأولوية: {m.Priority ?? "عادي"}\nالنوع: {(m.MeetingType == MeetingTypeEnum.Internal ? "داخلي" : "خارجي")}"
                }
            });
            return Ok(events);
        }

        // GET: api/meetings/5
        [HttpGet("api/meetings/{id}")]
        public async Task<IActionResult> GetMeeting(int id)
        {
            var meeting = await this._meetingService.GetMeetingByIdAsync(id);

            if (meeting == null)
                return NotFound(new { message = "الاجتماع غير موجود" });

            return Ok(meeting);
        }

        // GET: api/meetings/user/5
        [HttpGet("api/meetings/user/{userId}")]
        public async Task<IActionResult> GetUserMeetings(string userId)
        {
            var meetings = await _meetingService.GetUserMeetingsAsync(userId);
            return Ok(meetings);
        }

        // GET: api/meetings/today/5
        [HttpGet("api/meetings/today/{userId}")]
        public async Task<IActionResult> GetTodayMeetings(string userId)
        {
            var meetings = await _meetingService.GetTodayMeetingsAsync(userId);
            return Ok(meetings);
        }

        // GET: api/meetings/daterange?startDate=2025-01-01&endDate=2025-12-31
        [HttpGet("api/meetings/daterange")]
        public async Task<IActionResult> GetMeetingsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var meetings = await _meetingService.GetMeetingsByDateRangeAsync(startDate, endDate);
            return Ok(meetings);
        }


        // PUT: api/meetings/5
        [HttpPut("api/meetings/{id}")]
        public async Task<IActionResult> UpdateMeeting(int id, [FromBody] UpdateMeetingDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var meeting = await _meetingService.UpdateMeetingAsync(id, updateDto);

            if (meeting == null)
                return NotFound(new { message = "الاجتماع غير موجود" });

            return Ok(meeting);
        }

        // DELETE: api/meetings/5
        [HttpDelete("api/meetings/{id}")]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            var result = await _meetingService.DeleteMeetingAsync(id);

            if (!result)
                return NotFound(new { message = "الاجتماع غير موجود" });

            return Ok(new { message = "تم حذف الاجتماع بنجاح" });
        }

        // POST: api/meetings/5/invite
        [HttpPost("api/meetings/{id}/invite")]
        public async Task<IActionResult> InviteParticipants(int id, [FromBody] List<string> userIds)
        {
            var result = await _meetingService.InviteParticipantsAsync(id, userIds);

            if (!result)
                return NotFound(new { message = "الاجتماع غير موجود" });

            return Ok(new { message = "تم إرسال الدعوات بنجاح" });
        }

        // POST: api/meetings/5/rsvp
        [HttpPost("api/meetings/{id}/rsvp")]
        public async Task<IActionResult> RespondToInvitation(int id, [FromBody] RsvpDto rsvpDto)
        {
            // TODO: Get userId from authenticated user
            string userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier); // Temporary hardcoded value

            var result = await _meetingService.RespondToInvitationAsync(id, userId, rsvpDto.ResponseStatus);

            if (!result)
                return NotFound(new { message = "الدعوة غير موجودة" });

            return Ok(new { message = "تم تحديث حالة الحضور بنجاح" });
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
}
