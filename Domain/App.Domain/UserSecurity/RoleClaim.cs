using Microsoft.AspNetCore.Identity;

namespace App.Domain.UserSecurity
{ }
public class RoleClaim : IdentityRoleClaim<string>
{
    public string ClaimNameArabic { get; set; }
    public string Description { get; set; }

    public string CreatedById { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string LastModifiedById { get; set; }
    public DateTime? LastModifiedDate { get; set; } = DateTime.Now;
}

