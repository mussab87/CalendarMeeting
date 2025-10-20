
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace App.Helper.Dto { }

public class EditUserDto
{
    public EditUserDto()
    {
        Claims = new List<string>();
        Roles = new List<string>();
    }

    public string Id { get; set; }

    [Required]
    //[Display(Name = "Username")]
    public string UserName { get; set; }

    [Required(ErrorMessage = "First Name Field Required")]
    //[Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Last Name Field Required")]
    //[Display(Name = "Last Name")]
    public string LastName { get; set; }

    //[Display(Name = "First Name Arabic")]
    public string? FirstNameArabic { get; set; }

    //[Display(Name = "Last Name Arabic")]
    public string? LastNameArabic { get; set; }

    [Required(ErrorMessage = "Phone Number Field Required")]
    //[Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; }

    [Required]
    [EmailAddress]
    //[Remote(action: "IsEmailInUse", controller: "Account")]
    public string Email { get; set; }

    [Required(ErrorMessage = "User Status Field Required")]
    //[Display(Name = "User Status")]
    public bool? UserStatus { get; set; }

    public List<string> Claims { get; set; }

    public IList<string> Roles { get; set; }


}

