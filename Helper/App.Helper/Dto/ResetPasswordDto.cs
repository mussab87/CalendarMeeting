using System.ComponentModel.DataAnnotations;

namespace App.Helper.Dto
{ }
public class ResetPasswordDto
{
    [Display(Name = "اسم المستخدم")]
    public string Username { get; set; }
    public string Token { get; set; }
    public bool IsExpired { get; set; }
    public bool AdminResetUserPassword { get; set; }

    [Required(ErrorMessage = "حقل اجباري")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الحالية")]
    public string CurrentPassword { get; set; }

    [Required(ErrorMessage = "حقل اجباري")]
    [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل {2} أحرف وتحتوي على حرف صغير وحرف كبير ورموز ورقم.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الجديدة")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور الجديدة")]
    [Compare("NewPassword", ErrorMessage = "تأكيد كلمة المرور غير مطابقة")]
    public string ConfirmPassword { get; set; }
}

