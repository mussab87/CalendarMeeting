using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Helper.Dto
{ }
public class UserLoginLogDto
{
    public int? Id { get; set; }
    public string UserId { get; set; }

    public string ActivityType { get; set; } = "Login";
    public string Description { get; set; } = "Login";

    public string IpAddress { get; set; }
    public DateTime? LogginDateTime { get; set; }
    public DateTime? LoggedOutDateTime { get; set; }
}

