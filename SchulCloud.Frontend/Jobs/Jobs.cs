using Quartz;

namespace SchulCloud.Frontend.Jobs;

public static class Jobs
{
    public static JobKey LoginAttemptProcessJob => JobKey.Create("ProcessLoginAttemptJob");
}
