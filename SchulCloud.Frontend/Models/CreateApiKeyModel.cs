using SchulCloud.Authorization;

namespace SchulCloud.Frontend.Models;

public class CreateApiKeyModel
{
    public string Name { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime? Expires { get; set; }

    public Dictionary<string, PermissionLevel> PermissionLevels { get; set; } = [];
}
