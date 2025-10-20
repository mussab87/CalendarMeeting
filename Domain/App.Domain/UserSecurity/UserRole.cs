using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.UserSecurity
{ }
public class UserRole : IdentityUserRole<string>
{
    public DateTime? AssignedDate { get; set; } = DateTime.UtcNow;

    public int DepartmentId { get; set; }
    [ForeignKey("DepartmentId")]
    public virtual Department Department { get; set; }

    //Add more properties as needed
}
