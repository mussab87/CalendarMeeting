using App.Helper.Dto;
using System;
using System.Collections.Generic;

namespace App.Web.Models
{
    public class PrintCalendarViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<MeetingDto> Meetings { get; set; } = new List<MeetingDto>();
        public string UserName { get; set; }
        public DateTime PrintDate { get; set; }
        public string CompanyName { get; set; } = "نظام إدارة الاجتماعات";
        public string CompanyAddress { get; set; } = "المملكة العربية السعودية";
    }
}
