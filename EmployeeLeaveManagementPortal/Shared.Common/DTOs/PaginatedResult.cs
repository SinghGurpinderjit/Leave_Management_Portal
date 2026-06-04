namespace Shared.Common.DTOs;

public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    

    public static PaginatedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize) =>
        new()
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

    public static PaginatedResult<T> Empty(int page, int pageSize) =>
        new()
        {
            Items = [],
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
}