using App.Helper.Constants;
using App.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Text.Json;
using X.PagedList;
using X.PagedList.Extensions;

namespace App.Web.Controllers
{
    [PermissionAuthorize]
    public class CalendarController : BaseController
    {
        private readonly IMeetingService _meetingService;

        public CalendarController(IServiceProvider serviceProvider, IMeetingService meetingService) : base(serviceProvider)
        {
            _meetingService = meetingService;
        }

        // GET: /Calendar
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "Calendar";
            return View();
        }

        // GET: /Calendar/Meetings
        public async Task<IActionResult> Meetings(int page = 1, string searchString = "", int pageSize = 10)
        {
            ViewData["ActiveMenu"] = "CalendarMeetings";
            string userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get meetings based on user role
            IEnumerable<MeetingDto> allMeetings;
            if (User.IsInRole(Roles.SuperAdmin))
            {
                // SuperAdmin sees all meetings
                allMeetings = await _meetingService.GetAllMeetingsAsync();
            }
            else if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.OfficeManager) || User.IsInRole(Roles.Leader))
            {
                // Admin/OfficeManager/Leader sees department meetings + their own meetings with other departments
                var userDeptId = User.FindFirstValue("deptId");
                if (!string.IsNullOrEmpty(userDeptId) && int.TryParse(userDeptId, out int deptId))
                {
                    var departmentMeetings = await _meetingService.GetMeetingsByDepartmentAsync(deptId);
                    var userMeetings = await _meetingService.GetUserMeetingsAsync(userId);
                    
                    // Combine and remove duplicates based on meeting ID
                    allMeetings = departmentMeetings.Union(userMeetings, new MeetingDtoComparer()).ToList();
                }
                else
                {
                    // Fallback to user meetings if department ID is not available
                    allMeetings = await _meetingService.GetUserMeetingsAsync(userId);
                }
            }
            else
            {
                // Regular user sees only their meetings
                allMeetings = await _meetingService.GetUserMeetingsAsync(userId);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                allMeetings = allMeetings.Where(m =>
                    m.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    (m.Location != null && m.Location.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (m.OrganizerName != null && m.OrganizerName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Create paginated list
            var pagedMeetings = allMeetings.ToList().ToPagedList<MeetingDto>(page, pageSize);

            var model = new PaginatedResult<MeetingDto>
            {
                Items = pagedMeetings,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = pagedMeetings.TotalItemCount,
                SearchString = searchString
            };

            return View(model);
        }

        // GET: /Calendar/Today
        public async Task<IActionResult> Today()
        {
            string userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userDeptId = User.FindFirstValue("deptId");

            // Get meetings based on user role, then filter by today's date
            IEnumerable<MeetingDto> meetings;
            if (userRole == Roles.SuperAdmin)
            {
                // SuperAdmin sees all meetings
                meetings = await _meetingService.GetAllMeetingsAsync();
            }
            //else if (userRole == Roles.Admin || userRole == Roles.OfficeManager || userRole == Roles.Leader)
            //{
            //    // Admin/OfficeManager/Leader sees department meetings
            //    if (!string.IsNullOrEmpty(userDeptId) && int.TryParse(userDeptId, out int deptId))
            //    {
            //        meetings = await _meetingService.GetMeetingsByDepartmentAsync(deptId);
            //    }
            //    else
            //    {
            //        // Fallback to user meetings if department ID is not available
            //        meetings = await _meetingService.GetUserMeetingsAsync(userId);
            //    }
            //}
            else
            {
                // Regular user sees only their meetings
                meetings = await _meetingService.GetUserMeetingsAsync(userId);
            }

            // Filter by today's date
            var today = DateTime.Today;
            var todayMeetings = meetings.Where(m => m.StartTime.Date == today).ToList();

            return View(todayMeetings);
        }

        // GET: /Calendar/PrintCalendar
        public async Task<IActionResult> PrintCalendar(DateTime fromDate, DateTime toDate)
        {
            string userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get meetings for the date range
            var meetings = await _meetingService.GetMeetingsByDateRangeAsync(fromDate, toDate);

            // Filter meetings based on user role
            if (User.IsInRole(Roles.SuperAdmin))
            {
                // SuperAdmin sees all meetings - no filtering needed
            }
            else if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.OfficeManager) || User.IsInRole(Roles.Leader))
            {
                // Admin/OfficeManager/Leader sees department meetings + their own meetings with other departments
                var userDeptId = User.FindFirstValue("deptId");
                if (!string.IsNullOrEmpty(userDeptId) && int.TryParse(userDeptId, out int deptId))
                {
                    // Get department meetings
                    var departmentMeetings = await _meetingService.GetMeetingsByDepartmentAsync(deptId);
                    departmentMeetings = departmentMeetings.Where(m => m.StartTime.Date >= fromDate.Date && m.StartTime.Date <= toDate.Date).ToList();
                    
                    // Get user's own meetings in date range
                    var userMeetingsInRange = meetings.Where(m =>
                        m.OrganizerId == userId ||
                        m.Participants.Any(p => p.UserId == userId)
                    ).ToList();
                    
                    // Combine and remove duplicates
                    meetings = departmentMeetings.Union(userMeetingsInRange, new MeetingDtoComparer()).ToList();
                }
                else
                {
                    // Fallback to user meetings if department ID is not available
                    meetings = meetings.Where(m =>
                        m.OrganizerId == userId ||
                        m.Participants.Any(p => p.UserId == userId)
                    ).ToList();
                }
            }
            else
            {
                // Regular user sees only their meetings
                meetings = meetings.Where(m =>
                    m.OrganizerId == userId ||
                    m.Participants.Any(p => p.UserId == userId)
                ).ToList();
            }

            // Get user info for header - use the actual name from claims
            var userName = User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Name) ?? "المستخدم";

            var model = new PrintCalendarViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Meetings = meetings.ToList(),
                UserName = userName,
                PrintDate = DateTime.Now
            };

            return View(model);
        }

        // GET: /Calendar/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var meeting = await this._meetingService.GetMeetingByIdAsync(id);

            if (meeting == null)
                return NotFound();

            return View(meeting);
        }

        // GET: /Calendar/Create
        public async Task<IActionResult> Create()
        {
            // get user department by user id
            var userId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userDept = this._userService.GetUserRoles(userId).Result.FirstOrDefault();

            // get meeting location by user department
            this.ViewData["location"] = new SelectList(await this._unitOfWork.Repository<MeetingLocation>().GetAsync(u => u.DepartmentId == userDept.DepartmentId), "Id", "Location");

            // get participants
            this.ViewData["participants"] = new SelectList(await this._userService.GetAllUsers(), "Id", "Name");

            // get meeting priorities
            this.ViewData["priorities"] = new SelectList(await this._unitOfWork.Repository<MeetingPriority>().GetAsync(p => !p.IsDeleted), "Id", "Priority");

            return this.View();
        }

        [HttpPost]
        public IActionResult Create(CreateMeetingDto createDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                ViewBag.error = "يجب إدخال جميع البيانات!";
                return View(nameof(Create), createDto);
            }

            // TODO: Get organizerId from authenticated user
            string organizerId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            createDto.CreatedById = organizerId;
            //string organizerId = ""; // Temporary hardcoded value

            var meeting = _meetingService.CreateMeetingAsync(createDto, organizerId).Result;

            ViewBag.success = "تم حفظ بيانات الاجتماع بنجاح";
            return View(nameof(Create), createDto);
        }

        // GET: /Calendar/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var meeting = await _meetingService.GetMeetingByIdAsync(id);

            if (meeting == null)
                return NotFound();

            // get user department by user id
            var userId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userDept = this._userService.GetUserRoles(userId).Result.FirstOrDefault();

            // get meeting location by user department
            this.ViewData["location"] = new SelectList(await this._unitOfWork.Repository<MeetingLocation>().GetAsync(u => u.DepartmentId == userDept.DepartmentId), "Id", "Location");

            // get participants
            this.ViewData["participants"] = new SelectList(await this._userService.GetAllUsers(), "Id", "Name");

            // get meeting priorities
            this.ViewData["priorities"] = new SelectList(await this._unitOfWork.Repository<MeetingPriority>().GetAsync(p => !p.IsDeleted), "Id", "Priority");

            // Convert MeetingDto to UpdateMeetingDto
            var updateDto = new UpdateMeetingDto
            {
                Title = meeting.Title,
                Description = meeting.Description,
                Authority = meeting.Authority,
                MeetingPoints = meeting.MeetingPoints,
                StartTime = meeting.StartTime,
                EndTime = meeting.EndTime,
                LocationId = meeting.LocationId,
                MeetingType = meeting.MeetingType,
                RecurrenceRule = meeting.RecurrenceRule,
                IsRecurring = meeting.IsRecurring,
                MeetingStatusId = meeting.MeetingStatusId,
                PriorityId = meeting.PriorityId,
                ParticipantIds = meeting.Participants?.Select(p => p.UserId).ToList()
            };

            // Store meeting ID for the form
            this.ViewBag.MeetingId = id;

            return View(updateDto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int meetingId, UpdateMeetingDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate ViewData for dropdowns
                var userId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userDept = this._userService.GetUserRoles(userId).Result.FirstOrDefault();

                this.ViewData["location"] = new SelectList(await this._unitOfWork.Repository<MeetingLocation>().GetAsync(u => u.DepartmentId == userDept.DepartmentId), "Id", "Location");
                this.ViewData["participants"] = new SelectList(await this._userService.GetAllUsers(), "Id", "Name");
                this.ViewData["priorities"] = new SelectList(await this._unitOfWork.Repository<MeetingPriority>().GetAsync(p => !p.IsDeleted), "Id", "Priority");
                this.ViewBag.MeetingId = meetingId;

                ViewBag.error = "يرجى تصحيح الأخطاء في النموذج!";
                return View(updateDto);
            }

            try
            {
                var result = await _meetingService.UpdateMeetingAsync(meetingId, updateDto);

                if (result == null)
                {
                    ViewBag.error = "الاجتماع غير موجود!";
                    return View(updateDto);
                }

                ViewBag.success = "تم تحديث الاجتماع بنجاح!";

                // Repopulate ViewData for dropdowns after successful update
                var userId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userDept = this._userService.GetUserRoles(userId).Result.FirstOrDefault();

                this.ViewData["location"] = new SelectList(await this._unitOfWork.Repository<MeetingLocation>().GetAsync(u => u.DepartmentId == userDept.DepartmentId), "Id", "Location");
                this.ViewData["participants"] = new SelectList(await this._userService.GetAllUsers(), "Id", "Name");
                this.ViewData["priorities"] = new SelectList(await this._unitOfWork.Repository<MeetingPriority>().GetAsync(p => !p.IsDeleted), "Id", "Priority");
                this.ViewBag.MeetingId = meetingId;

                return View(updateDto);
            }
            catch (Exception ex)
            {
                ViewBag.error = "حدث خطأ أثناء تحديث الاجتماع!";

                // Repopulate ViewData for dropdowns
                var userId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userDept = this._userService.GetUserRoles(userId).Result.FirstOrDefault();

                this.ViewData["location"] = new SelectList(await this._unitOfWork.Repository<MeetingLocation>().GetAsync(u => u.DepartmentId == userDept.DepartmentId), "Id", "Location");
                this.ViewData["participants"] = new SelectList(await this._userService.GetAllUsers(), "Id", "Name");
                this.ViewData["priorities"] = new SelectList(await this._unitOfWork.Repository<MeetingPriority>().GetAsync(p => !p.IsDeleted), "Id", "Priority");
                this.ViewBag.MeetingId = meetingId;

                return View(updateDto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
        {
            try
            {
                var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userDept = this._userService.GetUserRoles(userId).Result.FirstOrDefault();

                if (userDept == null)
                    return BadRequest(new { success = false, message = "لم يتم العثور على القسم" });

                var newLocation = new MeetingLocation
                {
                    Location = request.LocationName,
                    DepartmentId = userDept.DepartmentId,
                    IsDeleted = false,
                    CreatedById = userId,
                    CreatedDate = DateTime.UtcNow
                };

                await this._unitOfWork.Repository<MeetingLocation>().AddAsync(newLocation);
                await this._unitOfWork.Complete(userId);

                return Ok(new
                {
                    success = true,
                    message = "تم إضافة الموقع بنجاح",
                    locationId = newLocation.Id,
                    locationName = newLocation.Location
                });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                //System.Diagnostics.Debug.WriteLine($"Error creating location: {ex.Message}");
                //System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return BadRequest(new { success = false, message = $"حدث خطأ أثناء إضافة الموقع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelMeeting([FromBody] CancelMeetingRequest request)
        {
            try
            {
                var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userDept = this._userService.GetUserRoles(userId).Result.FirstOrDefault();

                if (userDept == null)
                    return BadRequest(new { success = false, message = "لم يتم العثور على القسم" });

                // Get the meeting
                var meeting = await this._unitOfWork.Repository<Meeting>().GetByIdAsync(request.MeetingId);
                if (meeting == null)
                    return BadRequest(new { success = false, message = "الاجتماع غير موجود" });

                // Check if user is organizer
                var isOrganizer = await this._unitOfWork.Repository<MeetingParticipant>()
                    .GetAsync(p => p.MeetingId == request.MeetingId && p.UserId == userId && p.IsOrganizer);

                if (!isOrganizer.Any())
                    return BadRequest(new { success = false, message = "ليس لديك صلاحية لإلغاء هذا الاجتماع" });

                // Update meeting status to cancelled
                var cancelledStatus = await this._unitOfWork.Repository<MeetingStatus>()
                    .GetAsync(s => s.Status == "Cancelled" || s.Status == "ملغي");

                if (cancelledStatus.Any())
                {
                    meeting.MeetingStatusId = cancelledStatus.First().Id;
                    meeting.UpdatedAt = DateTime.UtcNow;
                    await this._unitOfWork.Repository<Meeting>().UpdateAsync(meeting);
                    await _unitOfWork.Complete(userId);

                    //System.Diagnostics.Debug.WriteLine($"Updated meeting {request.MeetingId} status to: {cancelledStatus.First().Status} (ID: {cancelledStatus.First().Id})");
                }
                else
                {
                    // If no cancelled status found, try to create one or use a default status
                    //System.Diagnostics.Debug.WriteLine("No cancelled status found in database");

                    // Try to find any status with ID 3 (assuming that's the cancelled status ID)
                    var status3 = await this._unitOfWork.Repository<MeetingStatus>().GetByIdAsync(3);
                    if (status3 != null)
                    {
                        meeting.MeetingStatusId = 3;
                        meeting.UpdatedAt = DateTime.UtcNow;
                        await this._unitOfWork.Repository<Meeting>().UpdateAsync(meeting);
                        await _unitOfWork.Complete(userId);

                        //System.Diagnostics.Debug.WriteLine($"Updated meeting {request.MeetingId} status to ID: 3 ({status3.Status})");
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "لم يتم العثور على حالة الإلغاء في النظام" });
                    }
                }

                // Get all participants for notification
                var participants = await this._unitOfWork.Repository<MeetingParticipant>()
                    .GetAsync(p => p.MeetingId == request.MeetingId);

                var participantIds = participants.Select(p => p.UserId).ToList();

                // Create notifications for all participants
                await CreateNotificationsForMeetingAsync(
                    request.MeetingId,
                    "تم إلغاء الاجتماع",
                    $"تم إلغاء الاجتماع: {meeting.Title}",
                    "Cancelled",
                    participantIds,
                    meeting.Title
                );

                // Save notifications to database
                await this._unitOfWork.Complete(userId);

                return Ok(new
                {
                    success = true,
                    message = "تم إلغاء الاجتماع بنجاح وإشعار جميع المشاركين"
                });
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error cancelling meeting: {ex.Message}");
                //System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return BadRequest(new { success = false, message = $"حدث خطأ أثناء إلغاء الاجتماع: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveMeetingResult([FromForm] SaveMeetingResultRequest request)
        {
            try
            {
                var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userDept = this._userService.GetUserRoles(userId).Result.FirstOrDefault();

                if (userDept == null)
                    return BadRequest(new { success = false, message = "لم يتم العثور على القسم" });

                // Get the meeting
                var meeting = await this._unitOfWork.Repository<Meeting>().GetByIdAsync(request.MeetingId);
                if (meeting == null)
                    return BadRequest(new { success = false, message = "الاجتماع غير موجود" });

                // Check if user is organizer
                var isOrganizer = await this._unitOfWork.Repository<MeetingParticipant>()
                    .GetAsync(p => p.MeetingId == request.MeetingId && p.UserId == userId && p.IsOrganizer);

                if (!isOrganizer.Any())
                    return BadRequest(new { success = false, message = "ليس لديك صلاحية لإضافة نتائج هذا الاجتماع" });

                // Save meeting finish note
                var finishNote = new MeetingFinishNote
                {
                    MeetingId = request.MeetingId,
                    UserId = userId,
                    NoteFinishContent = request.ResultContent,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = userId,
                    CreatedDate = DateTime.UtcNow
                };

                await this._unitOfWork.Repository<MeetingFinishNote>().AddAsync(finishNote);

                // Save the finish note first to get its ID
                await this._unitOfWork.Complete(userId);

                // The finishNote.Id should now have the generated ID from the database

                // Handle attendance data
                if (!string.IsNullOrEmpty(request.AttendanceData))
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var attendanceList = System.Text.Json.JsonSerializer.Deserialize<List<AttendanceData>>(request.AttendanceData, options);

                        // Get all participants for this meeting
                        var allParticipants = await this._unitOfWork.Repository<MeetingParticipant>()
                            .GetAsync(p => p.MeetingId == request.MeetingId);

                        // First, set all participants to not attended
                        foreach (var participant in allParticipants)
                        {
                            participant.IsAttended = false;
                            await this._unitOfWork.Repository<MeetingParticipant>().UpdateAsync(participant);
                        }

                        // Then, mark only the selected participants as attended
                        foreach (var attendance in attendanceList)
                        {
                            var participant = allParticipants.FirstOrDefault(p => p.UserId == attendance.UserId);
                            if (participant != null)
                            {
                                participant.IsAttended = true; // Only selected participants are in the list
                                await this._unitOfWork.Repository<MeetingParticipant>().UpdateAsync(participant);
                            }
                        }

                        // Save attendance changes to database
                        await this._unitOfWork.Complete(userId);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the entire operation
                        System.Diagnostics.Debug.WriteLine($"Error processing attendance data: {ex.Message}");
                    }
                }

                // Handle file attachments with validation
                if (request.Attachments != null && request.Attachments.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Processing {request.Attachments.Count} file attachments for MeetingFinishNote ID: {finishNote.Id}");
                    // Define allowed file types and extensions
                    var allowedContentTypes = new[]
                    {
                        "application/pdf",
                        "application/msword",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        "application/vnd.ms-excel",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "application/vnd.ms-powerpoint",
                        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                        "text/plain",
                        "image/jpeg",
                        "image/jpg",
                        "image/png"
                    };

                    var allowedExtensions = new[]
                    {
                        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png"
                    };

                    const long maxFileSize = 10 * 1024 * 1024; // 10MB

                    // Validate each file
                    foreach (var file in request.Attachments)
                    {
                        if (file.Length > 0)
                        {
                            // Check file size
                            if (file.Length > maxFileSize)
                            {
                                return BadRequest(new { success = false, message = $"حجم الملف كبير جداً: {file.FileName} (الحد الأقصى 10MB)" });
                            }

                            // Check file extension
                            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                return BadRequest(new { success = false, message = $"نوع الملف غير مدعوم: {file.FileName}" });
                            }

                            // Check content type
                            if (!allowedContentTypes.Contains(file.ContentType))
                            {
                                return BadRequest(new { success = false, message = $"نوع الملف غير مدعوم: {file.FileName}" });
                            }

                            // Additional security: Check file signature (magic numbers)
                            if (!IsValidFileSignature(file))
                            {
                                return BadRequest(new { success = false, message = $"الملف غير صالح أو تالف: {file.FileName}" });
                            }
                        }
                    }

                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "meeting-attachments");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    foreach (var file in request.Attachments)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                            var filePath = Path.Combine(uploadsPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var attachment = new MeetingAttachment
                            {
                                MeetingId = request.MeetingId,
                                MeetingFinishNoteId = finishNote.Id,
                                FileName = file.FileName,
                                FilePath = $"/uploads/meeting-attachments/{fileName}",
                                FileSize = file.Length,
                                ContentType = file.ContentType,
                                UploadedBy = userId,
                                UploadedAt = DateTime.UtcNow,
                                CreatedById = userId,
                                CreatedDate = DateTime.UtcNow
                            };

                            System.Diagnostics.Debug.WriteLine($"Creating attachment: {file.FileName} for MeetingFinishNote ID: {finishNote.Id}");
                            await this._unitOfWork.Repository<MeetingAttachment>().AddAsync(attachment);
                        }
                    }

                    // Save file attachments to database
                    await this._unitOfWork.Complete(userId);
                    System.Diagnostics.Debug.WriteLine("File attachments saved successfully");
                }

                // Update meeting status to "مكتمل" (Completed) - MeetingStatusId = 2
                meeting.MeetingStatusId = 2;
                await this._unitOfWork.Repository<Meeting>().UpdateAsync(meeting);
                await this._unitOfWork.Complete(userId);

                return Ok(new
                {
                    success = true,
                    message = "تم حفظ نتائج الاجتماع بنجاح"
                });
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error saving meeting result: {ex.Message}");
                //System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return BadRequest(new { success = false, message = $"حدث خطأ أثناء حفظ النتائج: {ex.Message}" });
            }
        }

        private async Task CreateNotificationsForMeetingAsync(int meetingId, string title, string message, string type, List<string> participantIds, string meetingTitle = null)
        {
            foreach (var participantId in participantIds)
            {
                // Check if this is a cancelled meeting notification
                if (type == "Cancelled")
                {
                    // First, update any existing notifications for this specific meeting (regardless of type or read status)
                    var existingNotifications = await this._unitOfWork.Repository<Notification>()
                        .GetAsync(n => n.UserId == participantId &&
                                     n.Message.Contains($"اجتماع جديد:") &&
                                     (!string.IsNullOrEmpty(meetingTitle) ? n.Message.Contains(meetingTitle) : true));

                    // Update existing notifications to cancelled
                    foreach (var existingNotification in existingNotifications)
                    {
                        var wasRead = existingNotification.IsRead;
                        var originalType = existingNotification.NotificationType;

                        existingNotification.Title = title;
                        existingNotification.Message = message;
                        existingNotification.NotificationType = "Cancelled";
                        existingNotification.IsRead = false; // Always set to unread so user sees the cancellation
                        existingNotification.CreatedAt = DateTime.UtcNow; // Update timestamp

                        await this._unitOfWork.Repository<Notification>().UpdateAsync(existingNotification);
                        await this._unitOfWork.Complete();

                        //System.Diagnostics.Debug.WriteLine($"Updated notification for user {participantId} from {originalType} (was read: {wasRead}) to Cancelled (now unread)");
                    }

                    // If no existing notification found, create a new one
                    if (!existingNotifications.Any())
                    {
                        var newNotification = new Notification
                        {
                            UserId = participantId,
                            Title = title,
                            Message = message,
                            NotificationType = type,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow,
                            CreatedById = participantId,
                            CreatedDate = DateTime.UtcNow
                        };

                        await this._unitOfWork.Repository<Notification>().AddAsync(newNotification);
                        await this._unitOfWork.Complete();

                        //System.Diagnostics.Debug.WriteLine($"Created new cancelled notification for user {participantId}");
                    }
                }
                else
                {
                    // For other notification types (like Invitation), create new notification
                    var notification = new Notification
                    {
                        UserId = participantId,
                        Title = title,
                        Message = message,
                        NotificationType = type,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedById = participantId,
                        CreatedDate = DateTime.UtcNow
                    };

                    await this._unitOfWork.Repository<Notification>().AddAsync(notification);
                }
            }
            // Note: Complete() should be called by the calling method after all notifications are added
        }

        [HttpGet]
        public async Task<IActionResult> GetAcceptedParticipants(int meetingId)
        {
            try
            {
                var participants = await _meetingService.GetAcceptedParticipantsAsync(meetingId);

                return Ok(new { success = true, participants = participants });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"حدث خطأ أثناء تحميل قائمة المشاركين: {ex.Message}" });
            }
        }

        // Helper method to validate file signatures
        private bool IsValidFileSignature(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var buffer = new byte[8];
                    stream.Read(buffer, 0, 8);
                    stream.Position = 0;

                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    // Check file signatures based on extension
                    switch (fileExtension)
                    {
                        case ".pdf":
                            return buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46; // %PDF
                        case ".doc":
                            return buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0; // DOC signature
                        case ".docx":
                            return buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04; // ZIP signature (DOCX is ZIP-based)
                        case ".xls":
                            return buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0; // XLS signature
                        case ".xlsx":
                            return buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04; // ZIP signature (XLSX is ZIP-based)
                        case ".ppt":
                            return buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0; // PPT signature
                        case ".pptx":
                            return buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04; // ZIP signature (PPTX is ZIP-based)
                        case ".txt":
                            return true; // Text files don't have specific signatures
                        case ".jpg":
                        case ".jpeg":
                            return buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF; // JPEG signature
                        case ".png":
                            return buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47; // PNG signature
                        default:
                            return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }


        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            try
            {
                var attachments = await this._unitOfWork.Repository<MeetingAttachment>().GetAsync(a => a.Id == id);
                var attachment = attachments.FirstOrDefault();

                if (attachment == null)
                {
                    return NotFound("الملف غير موجود");
                }

                // Handle different path formats
                var filePath = attachment.FilePath;
                if (filePath.StartsWith("/"))
                {
                    filePath = filePath.TrimStart('/');
                }
                if (filePath.StartsWith("wwwroot/"))
                {
                    filePath = filePath.Substring(8); // Remove "wwwroot/" prefix
                }

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);

                if (!System.IO.File.Exists(fullPath))
                {
                    // Try alternative path construction
                    var altPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(altPath))
                    {
                        fullPath = altPath;
                    }
                    else
                    {
                        return NotFound($"الملف غير موجود على الخادم. المسار: {fullPath}");
                    }
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                var fileName = attachment.FileName ?? "attachment";

                return File(fileBytes, attachment.ContentType ?? "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"حدث خطأ أثناء تحميل الملف: {ex.Message}");
            }
        }

        public class CreateLocationRequest
        {
            public string LocationName { get; set; }
        }

        public class CancelMeetingRequest
        {
            public int MeetingId { get; set; }
        }

        public class SaveMeetingResultRequest
        {
            public int MeetingId { get; set; }
            public string ResultContent { get; set; }
            public List<IFormFile> Attachments { get; set; }
            public string AttendanceData { get; set; }
        }

        public class AttendanceData
        {
            public string UserId { get; set; }
            public bool IsAttended { get; set; }
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
