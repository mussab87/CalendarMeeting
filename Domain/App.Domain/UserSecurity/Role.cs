using Microsoft.AspNetCore.Identity;

namespace App.Domain.UserSecurity { }

public class Role : IdentityRole<string>
{
    public Role() : base() { }
    public Role(string roleName) : base(roleName) { }
    public string RoleNameArabic { get; set; }
    public string Description { get; set; }

    public bool? IsDeleted { get; set; }

    public string CreatedById { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string LastModifiedById { get; set; }
    public DateTime? LastModifiedDate { get; set; } = DateTime.Now;
}

