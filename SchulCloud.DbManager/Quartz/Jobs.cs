using Quartz;

namespace SchulCloud.DbManager.Quartz;

public static class Jobs
{
    public static JobKey CleanerJob => JobKey.Create("CleanupJob");

    public static TriggerKey CleanerJobTimeTrigger => new("CleanupJob-timeTrigger");

    public static JobKey InitializerJob => JobKey.Create("InitializerJob");

    public static TriggerKey InitializerJobStartTrigger => new("InitializerJob-startTrigger");
}
