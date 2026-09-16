using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Generic container for paginated query responses.</summary>
    /// <typeparam name="T">The type of item contained in the page.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>The items on the current page.</summary>
        public List<T> Items { get; set; } = [];

        /// <summary>The current 1-based page number.</summary>
        public int Page { get; set; }

        /// <summary>The number of items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>The total number of items across all pages.</summary>
        public int TotalItems { get; set; }

        /// <summary>The total number of pages.</summary>
        public int TotalPages { get; set; }

        /// <summary>Indicates whether more items are available on subsequent pages.</summary>
        public bool HasNextPage { get; set; }

        /// <summary>Indicates whether there is a previous page available.</summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>Legacy alias for TotalItems.</summary>
        [JsonIgnore]
        public int TotalCount
        {
            get => TotalItems;
            set => TotalItems = value;
        }

        public PagedResult() { }

        public PagedResult(List<T> items, int totalItems, int page, int pageSize)
        {
            Items = items;
            TotalItems = totalItems;
            Page = page;
            PageSize = pageSize;
            TotalPages = totalItems == 0 || pageSize <= 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
            HasNextPage = TotalPages > 0 && page < TotalPages;
            HasPreviousPage = page > 1;
        }
    }
}
