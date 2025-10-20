using App.Domain.UserSecurity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities { }

public class UserLoginLog : EntityBase
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }

    public string ActivityType { get; set; } = "Login";
    public string Description { get; set; } = "Login";

    public string IpAddress { get; set; }
    public DateTime? LogginDateTime { get; set; }
    public DateTime? LoggedOutDateTime { get; set; }

}

