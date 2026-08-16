namespace DotnetMinimalApi.Common.Pagination;

/// <summary>
/// Query parameters for pagination and sorting across list endpoints.
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;
    private int _pageNumber = 1;

    public int? PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value is null or < 1 ? 1 : value.Value;
    }

    public int? PageSize
    {
        get => _pageSize;
        set => _pageSize = value is null or < 1 ? 10 : (value.Value > MaxPageSize ? MaxPageSize : value.Value);
    }

    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; } = false;
    public string? Search { get; set; }
}
