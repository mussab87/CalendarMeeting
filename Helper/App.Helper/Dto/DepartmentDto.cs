using System.ComponentModel.DataAnnotations;

namespace App.Helper.Dto { }
public class DepartmentDto
{
    public int Id { get; set; }

    [Display(Name = "اسم القسم:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public string Name { get; set; }

    [Display(Name = "وصف القسم:")]
    public string Description { get; set; }

    [Display(Name = "نوع القسم:")]
    [Required(ErrorMessage = "حقل إجباري")]
    public int DepartmentTypeId { get; set; }

    public string DepartmentTypeName { get; set; }

    [Display(Name = "حالة الحذف:")]
    public bool IsDeleted { get; set; }

    public string CreatedById { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string LastModifiedById { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    public int ActionType { get; set; } = (int)ActionTypeEnum.Add;
}
