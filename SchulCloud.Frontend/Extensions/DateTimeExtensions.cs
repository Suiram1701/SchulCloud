using System.Globalization;

namespace SchulCloud.Frontend.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Formats the datetime to a string displayed to the user.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ToDisplayedString(this DateTime dateTime)
    {
        return dateTime.ToString("d", CultureInfo.CurrentUICulture);
    }
}
