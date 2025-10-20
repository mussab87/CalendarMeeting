using X.PagedList;

namespace App.Helper.Paging
{ }
public class PaginatedResult<T> : PaginatedResultBase
{
    public IPagedList<T> Items { get; set; }
}

