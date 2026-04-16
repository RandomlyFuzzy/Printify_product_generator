using System;
using System.Collections.Generic;
using System.Linq;

namespace PrintifyGenerator.Dashboard.Models;

public sealed record PaginationState(
    int CurrentPage,
    int PageSize,
    int TotalItemCount,
    int TotalPageCount)
{
    public static PaginationState Empty { get; } = Create(1, 1, 0);

    public int SkipCount => (CurrentPage - 1) * PageSize;

    public int StartItemIndex => TotalItemCount == 0 ? 0 : SkipCount + 1;

    public int EndItemIndex => TotalItemCount == 0 ? 0 : Math.Min(SkipCount + PageSize, TotalItemCount);

    public int CurrentPageItemCount => TotalItemCount == 0 ? 0 : EndItemIndex - StartItemIndex + 1;

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPageCount;

    public IReadOnlyList<int> GetVisiblePageNumbers(int maxVisiblePages = 7)
    {
        var normalizedMaxVisiblePages = Math.Max(1, maxVisiblePages);
        if (TotalPageCount <= normalizedMaxVisiblePages)
        {
            return Enumerable.Range(1, TotalPageCount).ToList();
        }

        var halfWindow = normalizedMaxVisiblePages / 2;
        var startPage = Math.Max(1, CurrentPage - halfWindow);
        var endPage = startPage + normalizedMaxVisiblePages - 1;

        if (endPage > TotalPageCount)
        {
            endPage = TotalPageCount;
            startPage = Math.Max(1, endPage - normalizedMaxVisiblePages + 1);
        }

        return Enumerable.Range(startPage, endPage - startPage + 1).ToList();
    }

    public static PaginationState Create(int requestedPage, int pageSize, int totalItemCount)
    {
        var normalizedPageSize = Math.Max(1, pageSize);
        var normalizedTotalItemCount = Math.Max(0, totalItemCount);
        var totalPageCount = Math.Max(1, (int)Math.Ceiling(normalizedTotalItemCount / (double)normalizedPageSize));
        var normalizedRequestedPage = requestedPage <= 0 ? 1 : requestedPage;
        var currentPage = Math.Min(normalizedRequestedPage, totalPageCount);

        return new PaginationState(currentPage, normalizedPageSize, normalizedTotalItemCount, totalPageCount);
    }
}