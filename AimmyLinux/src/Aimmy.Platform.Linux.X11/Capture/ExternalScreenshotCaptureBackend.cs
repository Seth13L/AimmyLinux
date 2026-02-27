using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.Platform.Linux.X11.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Aimmy.Platform.Linux.X11.Capture;

public sealed class ExternalScreenshotCaptureBackend : ICaptureBackend
{
    private readonly ICommandRunner _commandRunner;
    private readonly string _preferredBackend;

    public ExternalScreenshotCaptureBackend(string preferredBackend, ICommandRunner? commandRunner = null)
    {
        _commandRunner = commandRunner ?? ProcessRunner.Instance;
        _preferredBackend = string.IsNullOrWhiteSpace(preferredBackend)
            ? "auto"
            : preferredBackend.Trim();
    }

    public string Name => $"ExternalCapture({_preferredBackend})";

    public async Task<Image<Rgba32>> CaptureAsync(CaptureRegion region, CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"aimmylinux_capture_{Guid.NewGuid():N}.png");
        var attempted = new List<string>();
        var failures = new List<string>();

        try
        {
            foreach (var (command, argsTemplate) in BuildCandidates(_preferredBackend))
            {
                if (!_commandRunner.CommandExists(command))
                {
                    continue;
                }

                attempted.Add(command);
                DeleteFileQuietly(tempFile);

                var args = argsTemplate
                    .Replace("{x}", region.X.ToString(), StringComparison.Ordinal)
                    .Replace("{y}", region.Y.ToString(), StringComparison.Ordinal)
                    .Replace("{w}", region.Width.ToString(), StringComparison.Ordinal)
                    .Replace("{h}", region.Height.ToString(), StringComparison.Ordinal)
                    .Replace("{file}", tempFile, StringComparison.Ordinal);

                var result = await _commandRunner.RunAsync(command, args, cancellationToken).ConfigureAwait(false);
                if (result.ExitCode != 0)
                {
                    failures.Add($"{command} exited with {result.ExitCode}.");
                    continue;
                }

                if (!File.Exists(tempFile))
                {
                    failures.Add($"{command} did not produce output.");
                    continue;
                }

                var info = new FileInfo(tempFile);
                if (info.Length == 0)
                {
                    failures.Add($"{command} produced an empty output.");
                    continue;
                }

                try
                {
                    return await Image.LoadAsync<Rgba32>(tempFile, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    failures.Add($"{command} produced invalid image data: {ex.Message}");
                }
            }

            var attemptedText = string.Join(", ", attempted.DefaultIfEmpty("none"));
            var failureText = failures.Count == 0 ? "none" : string.Join(" | ", failures);
            throw new InvalidOperationException(
                $"No capture backend succeeded. Attempted: {attemptedText}. Failures: {failureText}");
        }
        finally
        {
            DeleteFileQuietly(tempFile);
        }
    }

    private static IEnumerable<(string Command, string ArgumentsTemplate)> BuildCandidates(string preferredBackend)
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

        return preferredBackend.ToLowerInvariant() switch
        {
            "grim" => Grim(),
            "maim" => Maim(),
            "import" => Import(),
            "scrot" => Scrot(),
            _ => Grim().Concat(Maim()).Concat(Import()).Concat(Scrot())
        };
    }

    private static void DeleteFileQuietly(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore cleanup failures.
        }
    }
}
