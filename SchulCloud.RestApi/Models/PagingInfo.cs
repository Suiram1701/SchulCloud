namespace SchulCloud.RestApi.Models;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TItem"></typeparam>
public class PagingInfo<TItem>
{
    /// <summary>
    /// The items of the current page.
    /// </summary>
    public TItem[] Items { get; set; } = [];

    /// <summary>
    /// The index of the current page.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The size of the current page. If the size of <see cref="Items"/> is smaller than this this is the last page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total count of items.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// The total count of pages.
    /// </summary>
    public int TotalPages { get; set; }
}
