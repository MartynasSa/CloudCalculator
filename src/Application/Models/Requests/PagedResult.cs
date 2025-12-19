using System.Text.Json.Serialization;

namespace Application.Models.Dtos;

public class PaginationParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}


public class PricingRequest : PaginationParameters
{
    public string? VendorName { get; set; }
    public string? Service { get; set; }
    public string? Region { get; set; }
    public string? ProductFamily { get; set; }
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
