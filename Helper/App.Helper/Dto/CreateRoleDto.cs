using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace App.Helper.Dto
{

    public class CreateRoleDto
    {
        [Required(ErrorMessage = "Role Name Field Required")]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } // Removed 'required' keyword

        public List<IdentityRole>? Roles { get; set; }
    }

}

