using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.Platform.Linux.X11.Util;
using System.Text.RegularExpressions;

namespace Aimmy.Platform.Linux.X11.Display;

public sealed class X11DisplayDiscoveryService : IDisplayDiscoveryService
{
    private static readonly Regex ListMonitorsRegex = new(
        @"^\s*\d+:\s+\+(?<primary>\*)?(?<slot>\S+)\s+(?<w>\d+)\/\d+x(?<h>\d+)\/\d+\+(?<x>-?\d+)\+(?<y>-?\d+)\s+(?<id>\S+)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex QueryRegex = new(
        @"^(?<id>\S+)\s+connected(?:\s+primary)?\s+(?<w>\d+)x(?<h>\d+)\+(?<x>-?\d+)\+(?<y>-?\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ICommandRunner _commandRunner;

    public X11DisplayDiscoveryService(ICommandRunner? commandRunner = null)
    {
        _commandRunner = commandRunner ?? ProcessRunner.Instance;
    }

    public async Task<IReadOnlyList<DisplayInfo>> DiscoverAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() || !_commandRunner.CommandExists("xrandr"))
        {
            return Array.Empty<DisplayInfo>();
        }

        var listMonitorsResult = await _commandRunner.RunAsync("xrandr", "--listmonitors", cancellationToken).ConfigureAwait(false);
        if (listMonitorsResult.ExitCode == 0)
        {
            var monitors = ParseListMonitorsOutput(listMonitorsResult.StdOut);
            if (monitors.Count > 0)
            {
                return monitors;
            }
        }

        var queryResult = await _commandRunner.RunAsync("xrandr", "--query", cancellationToken).ConfigureAwait(false);
        if (queryResult.ExitCode == 0)
        {
            var connected = ParseQueryOutput(queryResult.StdOut);
            if (connected.Count > 0)
            {
                return connected;
            }
        }

        return Array.Empty<DisplayInfo>();
    }

    public static IReadOnlyList<DisplayInfo> ParseListMonitorsOutput(string stdOut)
    {
        if (string.IsNullOrWhiteSpace(stdOut))
        {
            return Array.Empty<DisplayInfo>();
        }

        var displays = new List<DisplayInfo>();
        var lines = stdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var match = ListMonitorsRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var id = match.Groups["id"].Value;
            var name = match.Groups["slot"].Value;
            var isPrimary = match.Groups["primary"].Success;
            var width = ParseInt(match.Groups["w"].Value, 0);
            var height = ParseInt(match.Groups["h"].Value, 0);
            var originX = ParseInt(match.Groups["x"].Value, 0);
            var originY = ParseInt(match.Groups["y"].Value, 0);

            if (width <= 0 || height <= 0 || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            displays.Add(new DisplayInfo(
                Id: id,
                Name: name,
                IsPrimary: isPrimary,
                OriginX: originX,
                OriginY: originY,
                Width: width,
                Height: height));
        }

        if (displays.Count == 0)
        {
            return displays;
        }

        if (!displays.Any(d => d.IsPrimary))
        {
            displays[0] = displays[0] with { IsPrimary = true };
        }

        return displays;
    }

    public static IReadOnlyList<DisplayInfo> ParseQueryOutput(string stdOut)
    {
        if (string.IsNullOrWhiteSpace(stdOut))
        {
            return Array.Empty<DisplayInfo>();
        }

        var displays = new List<DisplayInfo>();
        var lines = stdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(" connected", StringComparison.Ordinal))
            {
                continue;
            }

            var match = QueryRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var id = match.Groups["id"].Value;
            var isPrimary = line.Contains(" connected primary ", StringComparison.Ordinal);
            var width = ParseInt(match.Groups["w"].Value, 0);
            var height = ParseInt(match.Groups["h"].Value, 0);
            var originX = ParseInt(match.Groups["x"].Value, 0);
            var originY = ParseInt(match.Groups["y"].Value, 0);

            if (width <= 0 || height <= 0 || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            displays.Add(new DisplayInfo(
                Id: id,
                Name: id,
                IsPrimary: isPrimary,
                OriginX: originX,
                OriginY: originY,
                Width: width,
                Height: height));
        }

        if (displays.Count == 0)
        {
            return displays;
        }

        if (!displays.Any(d => d.IsPrimary))
        {
            displays[0] = displays[0] with { IsPrimary = true };
        }

        return displays;
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
