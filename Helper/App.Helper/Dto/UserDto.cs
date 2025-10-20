using System.ComponentModel.DataAnnotations;

namespace App.Helper.Dto { }
public class UserDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Display(Name = "اسم المستخدم:")]
    [Required(ErrorMessage = "حقل إجباري")]
    //[Remote(action: "IsUsernameInUse", controller: "SuperAdmin")]    
    public string Username { get; set; }

    [Display(Name = "الاسم:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public string Name { get; set; }

    [Display(Name = "النوع:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public int? DepartmentTypeId { get; set; }

    [Display(Name = "القسم:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public int? DepartmentId { get; set; }

    public string Department { get; set; }
    public string DepartmentType { get; set; }
    public string RoleName { get; set; }
    public string RoleNameArabic { get; set; }

    //[EmailAddress]
    //[Remote(action: "IsEmailInUse", controller: "SuperAdmin")]
    [Display(Name = "البريد الالكتروني:")]
    public string Email { get; set; } = $"{Guid.NewGuid().ToString()}@abc.com";

    public bool EmailConfirmed { get; set; } = false;

    public string PhoneNumber { get; set; } = "111111111";


    //[Required(ErrorMessage = "User Status Field Required")]
    //[Display(Name = "User Status")]
    public bool? UserStatus { get; set; } = true;

    public bool? FirstLogin { get; set; } = true;

    [Display(Name = "حالة الحساب:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public bool? IsActive { get; set; } = true;

    [Display(Name = "حذف المستخدم:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public bool? IsDeleted { get; set; } = false;

    public string CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string LastModifiedBy { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    [Display(Name = "صلاحية الحساب:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public string RoleId { get; set; }

    public int ActionType { get; set; } = (int)ActionTypeEnum.Add;

}

