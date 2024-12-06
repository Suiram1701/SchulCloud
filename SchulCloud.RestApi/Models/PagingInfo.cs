namespace SchulCloud.RestApi.Models;

/// <summary>
/// A wrapper around a pageable response.
/// </summary>
/// <typeparam name="TItem">The type of the item to page.</typeparam>
public class PagingInfo<TItem>
{
    /// <summary>
    /// The currently selected items.
    /// </summary>
    public TItem[] Items { get; set; } = [];

    /// <summary>
    /// The offset applied on this request.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Maximum amount of items to return per request. If the returned amount of items is smaller than this value the end of the collection is reached.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// The total amount of items available.
    /// </summary>
    public int TotalItems { get; set; }
}
