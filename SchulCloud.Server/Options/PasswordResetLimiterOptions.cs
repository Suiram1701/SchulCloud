namespace SchulCloud.Server.Options;

public class PasswordResetLimiterOptions
{
    public TimeSpan ResetTimeout { get; set; } = TimeSpan.FromMinutes(1);
}
