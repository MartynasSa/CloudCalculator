using System.Text.Json.Serialization;

namespace Application.Models.Dtos;

public class PaginationParameters
{
    /// <summary>
    /// 1-based page index.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Use a reasonable default.
    /// </summary>
    public int PageSize { get; set; } = 100;
}

public class PagedResult<T>
{
    public required List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    [JsonIgnore]
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public PagedResult()
    {
        Items = new List<T>();
    }

    public PagedResult(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        Items = items.ToList();
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
