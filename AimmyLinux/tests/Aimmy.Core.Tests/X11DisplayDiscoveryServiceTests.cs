using Aimmy.Platform.Linux.X11.Display;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class X11DisplayDiscoveryServiceTests
{
    [Fact]
    public void ParseListMonitorsOutput_ParsesPrimaryAndOffsets()
    {
        var output = """
            Monitors: 2
             0: +*HDMI-0 1920/531x1080/299+0+0  HDMI-0
             1: +DP-1 2560/597x1440/336+1920+0  DP-1
            """;

        var displays = X11DisplayDiscoveryService.ParseListMonitorsOutput(output);

        Assert.Equal(2, displays.Count);
        Assert.Equal("HDMI-0", displays[0].Id);
        Assert.True(displays[0].IsPrimary);
        Assert.Equal(1920, displays[0].Width);
        Assert.Equal(1080, displays[0].Height);
        Assert.Equal(0, displays[0].OriginX);
        Assert.Equal(0, displays[0].OriginY);

        Assert.Equal("DP-1", displays[1].Id);
        Assert.False(displays[1].IsPrimary);
        Assert.Equal(2560, displays[1].Width);
        Assert.Equal(1440, displays[1].Height);
        Assert.Equal(1920, displays[1].OriginX);
        Assert.Equal(0, displays[1].OriginY);
    }

    [Fact]
    public void ParseQueryOutput_ParsesConnectedDisplays()
    {
        var output = """
            Screen 0: minimum 8 x 8, current 4480 x 1440, maximum 32767 x 32767
            HDMI-0 connected primary 1920x1080+0+0 (normal left inverted right x axis y axis) 531mm x 299mm
            DP-1 connected 2560x1440+1920+0 (normal left inverted right x axis y axis) 597mm x 336mm
            DP-2 disconnected (normal left inverted right x axis y axis)
            """;

        var displays = X11DisplayDiscoveryService.ParseQueryOutput(output);

        Assert.Equal(2, displays.Count);
        Assert.Equal("HDMI-0", displays[0].Id);
        Assert.True(displays[0].IsPrimary);
        Assert.Equal("DP-1", displays[1].Id);
        Assert.False(displays[1].IsPrimary);
        Assert.Equal(2560, displays[1].Width);
        Assert.Equal(1440, displays[1].Height);
    }

    [Fact]
    public void ParseListMonitorsOutput_AssignsFirstDisplayAsPrimary_WhenNoneFlagged()
    {
        var output = """
            Monitors: 2
             0: +HDMI-0 1920/531x1080/299+0+0  HDMI-0
             1: +DP-1 1920/531x1080/299+1920+0  DP-1
            """;

        var displays = X11DisplayDiscoveryService.ParseListMonitorsOutput(output);

        Assert.Equal(2, displays.Count);
        Assert.True(displays[0].IsPrimary);
        Assert.False(displays[1].IsPrimary);
    }
}
