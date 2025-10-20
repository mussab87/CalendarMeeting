using System.ComponentModel.DataAnnotations;

namespace App.Helper.Dto
{ }
public class RoleDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Display(Name = "اسم الصلاحية:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public string Name { get; set; }
    [Display(Name = "اسم الصلاحية عربي:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public string RoleNameArabic { get; set; }
    [Display(Name = "وصف الصلاحية:")]
    public string Description { get; set; }
    [Display(Name = "ادخال بواسطة:")]
    public string CreatedById { get; set; }
    public DateTime? CreatedDate { get; set; }

    public string LastModifiedById { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    [Display(Name = "حذف الصلاحية:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public bool? IsDeleted { get; set; } = false;

    public int ActionType { get; set; } = (int)ActionTypeEnum.Add;
}

