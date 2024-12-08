using System.Reflection;

namespace SchulCloud.RestApi.Sorting;

/// <summary>
/// A critiria to sort by.
/// </summary>
/// <param name="Property">The property to sort by.</param>
/// <param name="Direction">The sort direction.</param>
public sealed record SortingCriteria(PropertyInfo Property, SortingDirection Direction);