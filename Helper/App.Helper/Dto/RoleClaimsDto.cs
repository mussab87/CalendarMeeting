
namespace App.Helper.Dto { }
public class RoleClaimsDto
{
    public RoleClaimsDto()
    {
        Cliams = new List<RolePermission>();
    }

    public string RoleId { get; set; }
    public string RoleName { get; set; }
    public string RoleNameArabic { get; set; }
    public List<RolePermission> Cliams { get; set; }
}

