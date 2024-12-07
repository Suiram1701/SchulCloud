namespace SchulCloud.RestApi.Filtering;

/// <summary>
/// The different filter operations that can be applied to a collection.
/// </summary>
public enum FilterOperators
{
    /// <summary>
    /// Equal
    /// </summary>
    Eq,

    /// <summary>
    /// Not equal
    /// </summary>
    Ne,

    /// <summary>
    /// Greater than
    /// </summary>
    Gt,

    /// <summary>
    /// Less than
    /// </summary>
    Lt,

    /// <summary>
    /// Greater or equal than
    /// </summary>
    Gte,

    /// <summary>
    /// Less or equal than
    /// </summary>
    Lte,

    /// <summary>
    /// String comparison
    /// </summary>
    Like,

    /// <summary>
    /// Case-insensitive string comparison
    /// </summary>
    ILike
}
