using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace App.Helper.Dto
{
    public class ChangePasswordDto
    {
        public string Id { get; set; }
        //[Display(Name = "Username")]
        public string username { get; set; }
        public string Name { get; set; }

        [Required(ErrorMessage = "New Password Field Required")]
        [DataType(DataType.Password)]
        //[Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        //[Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage =
            "New Password and Confirm New Password not match")]
        public string ConfirmPassword { get; set; }
    }
}

