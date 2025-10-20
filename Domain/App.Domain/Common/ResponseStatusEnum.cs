namespace App.Domain.Common { }

public enum ResponseStatusEnum
{
    /// <summary>
    /// في انتظار الموافقة
    /// </summary>
    Pending = 0,

    /// <summary>
    /// تمت الموافقة
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// تم الرفض
    /// </summary>
    Declined = 2,
}

