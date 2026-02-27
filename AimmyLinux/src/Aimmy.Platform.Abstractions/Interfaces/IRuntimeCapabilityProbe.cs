using Aimmy.Core.Capabilities;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IRuntimeCapabilityProbe
{
    RuntimeCapabilities Probe();
}
