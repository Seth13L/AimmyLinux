namespace Aimmy.Platform.Linux.X11.Util;

public interface ICommandRunner
{
    bool CommandExists(string command);

    Task<CommandResult> RunAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken);
}
