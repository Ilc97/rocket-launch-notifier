using RocketLaunchNotifier.Models;

public enum LaunchChangeType
{
    NEW,
    RESCHEDULED,
    STATUS_CHANGE,
    POSTPONED
}

public class LaunchChange
{
    public Launch Launch { get; set; }
    public LaunchChangeType ChangeType { get; set; }

    public LaunchChange(Launch launch, LaunchChangeType changeType)
    {
        Launch = launch;
        ChangeType = changeType;
    }
}