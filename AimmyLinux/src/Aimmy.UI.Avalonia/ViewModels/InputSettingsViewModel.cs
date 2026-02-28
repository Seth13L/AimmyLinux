using Aimmy.Core.Config;
using Aimmy.Core.Enums;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class InputSettingsViewModel
{
    public IReadOnlyList<string> PreferredMethodOptions { get; } = Enum.GetNames<InputMethod>();

    public string PreferredMethod { get; set; } = InputMethod.UInput.ToString();
    public string AimKeybind { get; set; } = string.Empty;
    public string SecondaryAimKeybind { get; set; } = string.Empty;
    public string DynamicFovKeybind { get; set; } = string.Empty;
    public string EmergencyStopKeybind { get; set; } = string.Empty;
    public string ModelSwitchKeybind { get; set; } = string.Empty;

    public void Load(AimmyConfig config)
    {
        PreferredMethod = config.Input.PreferredMethod.ToString();
        AimKeybind = config.Input.AimKeybind;
        SecondaryAimKeybind = config.Input.SecondaryAimKeybind;
        DynamicFovKeybind = config.Input.DynamicFovKeybind;
        EmergencyStopKeybind = config.Input.EmergencyStopKeybind;
        ModelSwitchKeybind = config.Input.ModelSwitchKeybind;
    }

    public void Apply(AimmyConfig config)
    {
        if (Enum.TryParse<InputMethod>(PreferredMethod, ignoreCase: true, out var preferredMethod))
        {
            config.Input.PreferredMethod = preferredMethod;
        }

        config.Input.AimKeybind = AimKeybind;
        config.Input.SecondaryAimKeybind = SecondaryAimKeybind;
        config.Input.DynamicFovKeybind = DynamicFovKeybind;
        config.Input.EmergencyStopKeybind = EmergencyStopKeybind;
        config.Input.ModelSwitchKeybind = ModelSwitchKeybind;
    }
}
