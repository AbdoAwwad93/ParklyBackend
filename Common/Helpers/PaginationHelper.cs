using System;
using System.Collections.Generic;
using System.Linq;

namespace Parkly_Backend.Common.Helpers
{
    /// <summary>Helper providing offset-based pagination logic for in-memory collections.</summary>
    public static class PaginationHelper
    {
        /// <summary>
        /// Paginates an in-memory collection using page and pageSize, returning paged items and metadata.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The full ordered collection.</param>
        /// <param name="page">The requested page number (1-indexed, defaults to 1).</param>
        /// <param name="pageSize">The requested page size (defaults to 20).</param>
        /// <returns>A tuple of paged items, effective page, effective pageSize, totalItems, totalPages, hasNextPage, and hasPreviousPage.</returns>
        public static (List<T> Items, int Page, int PageSize, int TotalItems, int TotalPages, bool HasNextPage, bool HasPreviousPage) Paginate<T>(
            List<T> source,
            int page,
            int pageSize)
        {
            page = page > 0 ? page : 1;
            pageSize = pageSize > 0 ? pageSize : 20;
            int totalItems = source.Count;
            int skip = (page - 1) * pageSize;

            skip = Math.Clamp(skip, 0, totalItems);
            var pagedItems = source.Skip(skip).Take(pageSize).ToList();

            int totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
            bool hasNextPage = totalPages > 0 && page < totalPages;
            bool hasPreviousPage = page > 1;

            return (pagedItems, page, pageSize, totalItems, totalPages, hasNextPage, hasPreviousPage);
        }
    }
}
