namespace FoamStudio.Core;

public sealed record Command(string FileName, IReadOnlyList<string> Arguments, bool UnicodeOutput = false);
public sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
public sealed record LogLine(string Text, bool IsError = false);

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(Command command, Action<LogLine>? output = null,
        CancellationToken cancellationToken = default);
}

public sealed record OpenFoamEnvironment(string Distribution, string Bashrc, string Version,
    string Tutorial, bool HasParaView, bool Ready, string Details);

public sealed record SimulationResult(int ExitCode, bool Cancelled, string? CasePath);
