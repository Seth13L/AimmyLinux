using Aimmy.Core.Config;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class TriggerSettingsViewModel
{
    public bool Enabled { get; set; }
    public bool SprayMode { get; set; }
    public bool CursorCheck { get; set; }
    public double AutoTriggerDelaySeconds { get; set; }

    public void Load(AimmyConfig config)
    {
        Enabled = config.Trigger.Enabled;
        SprayMode = config.Trigger.SprayMode;
        CursorCheck = config.Trigger.CursorCheck;
        AutoTriggerDelaySeconds = config.Trigger.AutoTriggerDelaySeconds;
    }

    public void Apply(AimmyConfig config)
    {
        config.Trigger.Enabled = Enabled;
        config.Trigger.SprayMode = SprayMode;
        config.Trigger.CursorCheck = CursorCheck;
        config.Trigger.AutoTriggerDelaySeconds = AutoTriggerDelaySeconds;
    }
}
