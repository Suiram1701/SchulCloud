namespace SchulCloud.Identity.Options;

/// <summary>
/// Options for user api keys.
/// </summary>
public class ApiKeyOptions
{
    /// <summary>
    /// A prefix to use for each generated key. The key will look like '{prefix}-{key}'
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// The length of the generated key.
    /// </summary>
    public int KeyLength { get; set; } = 48;

    /// <summary>
    /// Chars that will be used to generate an api key.
    /// </summary>
    public string AllowedChars { get; set; } = "aAbBcCdDeEfFgGhHiIjJkKlLmMnNoOpPqQrRsStTuUvVwWxXyYzZ0123456789";

    /// <summary>
    /// A count of api keys allowed for each user. 
    /// 0 means users are not permitted to has api keys. 
    /// -1 means there is no limit.
    /// </summary>
    /// <remarks>
    /// Changing this does not effect existing keys.
    /// </remarks>
    public int MaxKeysPerUser { get; set; } = -1;

    /// <summary>
    /// A salt to use to hash every api key. By changing this every existing key will be invalidated.
    /// </summary>
    public string GlobalSalt { get; set; } = Guid.NewGuid().ToString();
}
