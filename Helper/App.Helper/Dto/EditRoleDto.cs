
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace App.Helper.Dto { }

public class EditRoleDto
{
    public EditRoleDto()
    {
        Users = new List<string>();
    }

    [Display(Name = "Role Id")]
    public string Id { get; set; }

    [Required(ErrorMessage = "Role Name Field Required")]
    [Display(Name = "Role Name")]
    public string RoleName { get; set; }

    public List<string>? Users { get; set; }
}

