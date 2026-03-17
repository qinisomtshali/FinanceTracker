namespace FinanceTracker.Application.Common.Models;

/// <summary>
/// Wraps a page of results with pagination metadata.
/// Every list endpoint should return this — not raw lists.
/// 
/// WHY: The frontend needs to know total count to show page numbers,
/// and HasNextPage/HasPreviousPage for navigation. Without this,
/// the frontend would have to fetch ALL records to know the count.
/// </summary>
public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
