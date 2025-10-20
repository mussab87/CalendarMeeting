using X.PagedList;

namespace App.Helper.Paging
{ }
public class PaginatedResultBase
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string SearchString { get; set; }

    public UserDto UserDto { get; set; }
    public RoleDto RoleDto { get; set; }
    public DepartmentDto DepartmentDto { get; set; }
    public MeetingDto MeetingDto { get; set; }
}

