using App.Domain.UserSecurity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Common { }
public abstract class EntityBase()
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key, Column(Order = 0)]
    public int Id { get; set; }
    public string CreatedById { get; set; }
    [ForeignKey("CreatedById")]
    [NotMapped]
    public virtual User CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string LastModifiedById { get; set; }
    [ForeignKey("LastModifiedById")]
    [NotMapped]
    public virtual User LastModifiedBy { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}

