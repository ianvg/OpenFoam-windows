using System.Collections.Concurrent;
using FoamStudio.Core;
using FoamStudio.Infrastructure;
using FoamStudio.OpenFoam;
using Xunit;

namespace FoamStudio.Tests;

public class ExecutionTests
{
    [Fact]
    public async Task CapturesBothStreamsAndNonzeroExit()
    {
        var lines = new ConcurrentBag<LogLine>();
        var result = await new ProcessRunner().RunAsync(new Command("powershell.exe",
            ["-NoProfile", "-Command", "[Console]::Out.WriteLine('output'); [Console]::Error.WriteLine('failure'); exit 7"]), lines.Add);
        Assert.Equal(7, result.ExitCode);
        Assert.Contains(lines, x => x.Text == "output" && !x.IsError);
        Assert.Contains(lines, x => x.Text == "failure" && x.IsError);
    }

    [Fact]
    public async Task WindowsProcessCancellationCompletes()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ProcessRunner().RunAsync(
            new Command("powershell.exe", ["-NoProfile", "-Command", "Start-Sleep -Seconds 120"]),
            cancellationToken: cancellation.Token));
    }

    [Theory]
    [InlineData("VERSION=13\nTUTORIAL=/tutorial\nPARAVIEW=yes\n", 0, true)]
    [InlineData("VERSION=2412\n", 3, false)]
    [InlineData("", 2, false)]
    public async Task EnvironmentReadinessFollowsProbeExit(string output, int exit, bool expected)
    {
        var service = new OpenFoamService(new StubRunner(new(exit, output, "diagnostic")));
        var found = await service.ProbeAsync("Ubuntu", "/path with spaces/it's/bashrc");
        Assert.Equal(expected, found.Ready);
        Assert.Contains("diagnostic", found.Details);
    }

    [Fact]
    public async Task UnsupportedEnvironmentCannotStartSimulation()
    {
        var runner = new StubRunner(new(0, "", ""));
        var service = new OpenFoamService(runner);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunCavityAsync(
            new("Ubuntu", "/bashrc", "2412", "", false, true, ""), _ => { }));
        Assert.Equal(0, runner.CallCount);
    }

    [Fact]
    public async Task Wsl1IsRejectedBeforeStartingLinux()
    {
        var runner = new StubRunner(new(0, "", ""), "1");
        var found = await new OpenFoamService(runner).ProbeAsync("Ubuntu", "/bashrc");
        Assert.False(found.Ready);
        Assert.Contains("WSL2", found.Details);
        Assert.Equal(1, runner.CallCount);
    }

    [Fact]
    public async Task DistributionsHandleUnicodeNulAndCrLf()
    {
        var service = new OpenFoamService(new StubRunner(new(0, "Ubuntu\0\r\nUbuntu-22.04\r\n", "")));
        Assert.Equal(["Ubuntu", "Ubuntu-22.04"], await service.ListDistributionsAsync());
    }

    private sealed class StubRunner(CommandResult result, string wslVersion = "2") : ICommandRunner
    {
        public int CallCount { get; private set; }
        public Task<CommandResult> RunAsync(Command command, Action<LogLine>? output = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (command.Arguments.Contains("--verbose"))
                return Task.FromResult(new CommandResult(0, $"  NAME    STATE    VERSION\r\n* Ubuntu    Stopped    {wslVersion}\r\n", ""));
            return Task.FromResult(result);
        }
    }
}
