using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{ }
public class MeetingLocation : EntityBase
{
    public int DepartmentId { get; set; }
    [ForeignKey("DepartmentId")]
    public virtual Department Department { get; set; }
    public string Location { get; set; }
    public bool IsDeleted { get; set; } = false;
}

