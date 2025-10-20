
using System.ComponentModel.DataAnnotations;

namespace App.Domain.Entities { }

public class MeetingStatus : EntityBase
{
    [MaxLength(200)]
    public string Status { get; set; }
}

