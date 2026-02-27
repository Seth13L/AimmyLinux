namespace Aimmy.Platform.Linux.X11.Util;

public sealed record CommandResult(int ExitCode, string StdOut, string StdErr);
