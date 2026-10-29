using System.Diagnostics;
using System.Text;
using FoamStudio.Core;

namespace FoamStudio.Infrastructure;

public sealed class ProcessRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(Command command, Action<LogLine>? output = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = new ProcessStartInfo(command.FileName)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = command.UnicodeOutput ? Encoding.Unicode : Encoding.UTF8,
            StandardErrorEncoding = command.UnicodeOutput ? Encoding.Unicode : Encoding.UTF8
        };
        foreach (var argument in command.Arguments) info.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = info };
        process.Start();
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        });
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        async Task ReadAsync(StreamReader reader, StringBuilder buffer, bool error)
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                buffer.AppendLine(line);
                output?.Invoke(new LogLine(line, error));
            }
        }
        var reads = Task.WhenAll(ReadAsync(process.StandardOutput, stdout, false),
            ReadAsync(process.StandardError, stderr, true));
        await process.WaitForExitAsync(CancellationToken.None);
        await reads;
        cancellationToken.ThrowIfCancellationRequested();
        return new CommandResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
