namespace App.Domain.Entities
{ }
public class MeetingPriority : EntityBase
{
    public string Priority { get; set; }
    public string PriorityColor { get; set; }
    public bool IsDeleted { get; set; } = false;
}

