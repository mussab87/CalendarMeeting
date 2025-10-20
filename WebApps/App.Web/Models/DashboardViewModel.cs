using System;
using System.Collections.Generic;

namespace App.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalMeetings { get; set; }
        public int TodayMeetings { get; set; }
        public int CompletedMeetings { get; set; }
        public int PendingMeetings { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDepartments { get; set; }
        public List<MeetingStatsByMonth> MeetingStatsByMonth { get; set; } = new List<MeetingStatsByMonth>();
        public List<MeetingStatsByPriority> MeetingStatsByPriority { get; set; } = new List<MeetingStatsByPriority>();
        public List<MeetingStatsByStatus> MeetingStatsByStatus { get; set; } = new List<MeetingStatsByStatus>();
        public List<UpcomingMeeting> UpcomingMeetings { get; set; } = new List<UpcomingMeeting>();
        public string UserRole { get; set; }
        public string UserDepartment { get; set; }
    }

    public class MeetingStatsByMonth
    {
        public string Month { get; set; }
        public int Count { get; set; }
    }

    public class MeetingStatsByPriority
    {
        public string Priority { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
    }

    public class MeetingStatsByStatus
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
    }

    public class UpcomingMeeting
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; }
        public string Priority { get; set; }
        public string OrganizerName { get; set; }
    }
}
