using App.Domain.UserSecurity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities { }

public class UserPasswordLog : EntityBase
{
    public string UserId { get; set; }
    public User User { get; set; }
    public DateTime PasswordChange { get; set; }
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}

