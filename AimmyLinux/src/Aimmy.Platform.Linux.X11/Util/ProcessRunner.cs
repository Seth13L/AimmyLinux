using System.Diagnostics;

namespace Aimmy.Platform.Linux.X11.Util;

public sealed class ProcessRunner : ICommandRunner
{
    public static ProcessRunner Instance { get; } = new();

    private ProcessRunner()
    {
    }

    public bool CommandExists(string command)
    {
        var lookup = OperatingSystem.IsWindows() ? "where" : "which";

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = lookup,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<CommandResult> RunAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new CommandResult(
            process.ExitCode,
            await stdOutTask.ConfigureAwait(false),
            await stdErrTask.ConfigureAwait(false));
    }
}
