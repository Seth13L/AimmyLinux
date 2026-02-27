using Aimmy.Core.Config;

namespace Aimmy.Core.Targeting;

public static class DynamicFovResolver
{
    public static int Resolve(AimmyConfig config, bool dynamicFovKeyPressed)
    {
        var baseSize = Math.Max(1, config.Fov.Size);
        if (!config.Fov.Enabled)
        {
            return baseSize;
        }

        if (config.Aim.DynamicFovEnabled && dynamicFovKeyPressed)
        {
            return Math.Max(1, config.Fov.DynamicSize);
        }

        return baseSize;
    }
}
