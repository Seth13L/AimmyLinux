using Aimmy.Core.Capabilities;
using Aimmy.Core.Diagnostics;
using Aimmy.UI.Avalonia.Models;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class RuntimeStatusViewModel
{
    public List<CapabilityBadgeModel> Capabilities { get; } = new();
    public RuntimeSnapshot Snapshot { get; private set; }

    public void UpdateCapabilities(RuntimeCapabilities runtimeCapabilities)
    {
        Capabilities.Clear();
        foreach (var item in runtimeCapabilities.Features.Values.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
        {
            Capabilities.Add(new CapabilityBadgeModel(item.Name, item.State, item.IsDegraded, item.Message));
        }
    }

    public void UpdateSnapshot(RuntimeSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}
