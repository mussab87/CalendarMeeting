using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{ }
public class Department : EntityBase
{
    public int DepartmentTypeId { get; set; }
    [ForeignKey("DepartmentTypeId")]
    public virtual DepartmentType DepartmentType { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsDeleted { get; set; } = false;
}

