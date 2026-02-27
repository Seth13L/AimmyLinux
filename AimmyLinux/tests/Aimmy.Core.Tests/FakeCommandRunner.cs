using Aimmy.Platform.Linux.X11.Util;

namespace Aimmy.Core.Tests;

internal sealed class FakeCommandRunner : ICommandRunner
{
    private readonly Func<string, bool> _commandExists;
    private readonly Func<string, string, CancellationToken, CommandResult> _runCommand;

    public FakeCommandRunner(
        Func<string, bool>? commandExists = null,
        Func<string, string, CancellationToken, CommandResult>? runCommand = null)
    {
        _commandExists = commandExists ?? (_ => false);
        _runCommand = runCommand ?? ((_, _, _) => new CommandResult(1, string.Empty, "failed"));
    }

    public List<string> RunOrder { get; } = new();

    public bool CommandExists(string command)
    {
        return _commandExists(command);
    }

    public Task<CommandResult> RunAsync(string command, string arguments, CancellationToken cancellationToken)
    {
        RunOrder.Add(command);
        return Task.FromResult(_runCommand(command, arguments, cancellationToken));
    }
}
