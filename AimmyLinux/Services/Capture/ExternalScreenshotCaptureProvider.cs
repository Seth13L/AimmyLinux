using AimmyLinux.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AimmyLinux.Services.Capture;

public sealed class ExternalScreenshotCaptureProvider : ICaptureProvider
{
    private readonly string _preferredBackend;

    public ExternalScreenshotCaptureProvider(string preferredBackend)
    {
        _preferredBackend = preferredBackend;
    }

    public async Task<Image<Rgba32>> CaptureAsync(CaptureRegion region, CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"aimmylinux_{Guid.NewGuid():N}.png");
        var attempted = new List<string>();

        try
        {
            foreach (var (command, argsTemplate) in BuildCandidates())
            {
                if (!ProcessRunner.CommandExists(command))
                {
                    continue;
                }

                var args = argsTemplate
                    .Replace("{x}", region.X.ToString(), StringComparison.Ordinal)
                    .Replace("{y}", region.Y.ToString(), StringComparison.Ordinal)
                    .Replace("{w}", region.Width.ToString(), StringComparison.Ordinal)
                    .Replace("{h}", region.Height.ToString(), StringComparison.Ordinal)
                    .Replace("{file}", tempFile, StringComparison.Ordinal);

                attempted.Add(command);
                var exitCode = await ProcessRunner.RunAsync(command, args, cancellationToken).ConfigureAwait(false);
                if (exitCode != 0 || !File.Exists(tempFile))
                {
                    continue;
                }

                if (new FileInfo(tempFile).Length == 0)
                {
                    continue;
                }

                return await Image.LoadAsync<Rgba32>(tempFile, cancellationToken).ConfigureAwait(false);
            }

            throw new InvalidOperationException(
                $"No supported screenshot backend succeeded. Attempted: {string.Join(", ", attempted.DefaultIfEmpty("none"))}");
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // Ignore best-effort cleanup failures.
            }
        }
    }

    private IEnumerable<(string Command, string Arguments)> BuildCandidates()
    {
        static IEnumerable<(string, string)> Grim()
        {
            yield return ("grim", "-g \"{x},{y} {w}x{h}\" \"{file}\"");
        }

        static IEnumerable<(string, string)> Maim()
        {
            yield return ("maim", "-g \"{w}x{h}+{x}+{y}\" \"{file}\"");
        }

        static IEnumerable<(string, string)> Import()
        {
            yield return ("import", "-window root -crop \"{w}x{h}+{x}+{y}\" \"{file}\"");
        }

        static IEnumerable<(string, string)> Scrot()
        {
            yield return ("scrot", "-a \"{x},{y},{w},{h}\" \"{file}\"");
        }

        if (_preferredBackend.Equals("grim", StringComparison.OrdinalIgnoreCase))
        {
            return Grim();
        }

        if (_preferredBackend.Equals("maim", StringComparison.OrdinalIgnoreCase))
        {
            return Maim();
        }

        if (_preferredBackend.Equals("import", StringComparison.OrdinalIgnoreCase))
        {
            return Import();
        }

        if (_preferredBackend.Equals("scrot", StringComparison.OrdinalIgnoreCase))
        {
            return Scrot();
        }

        // Auto mode fallback order.
        return Grim().Concat(Maim()).Concat(Import()).Concat(Scrot());
    }
}
