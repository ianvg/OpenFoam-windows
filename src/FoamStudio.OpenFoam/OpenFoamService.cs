using System.Globalization;
using FoamStudio.Core;
using FoamStudio.Infrastructure;

namespace FoamStudio.OpenFoam;

public sealed class OpenFoamService(ICommandRunner runner)
{
    public const string DefaultBashrc = "/opt/openfoam13/etc/bashrc";

    public async Task<string[]> ListDistributionsAsync(CancellationToken token = default)
    {
        var result = await runner.RunAsync(WslCommands.ListDistributions(), cancellationToken: token);
        if (result.ExitCode != 0) throw new InvalidOperationException(result.StandardError + result.StandardOutput);
        return result.StandardOutput.Replace("\0", "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
    }

    public async Task<OpenFoamEnvironment> ProbeAsync(string distribution, string bashrc,
        CancellationToken token = default)
    {
        var versions = await runner.RunAsync(WslCommands.ListVersions(), cancellationToken: token);
        if (versions.ExitCode != 0)
            return new(distribution, bashrc, "", "", false, false,
                "Could not inspect WSL versions. " + versions.StandardError + versions.StandardOutput);
        var distributionLine = versions.StandardOutput.Replace("\0", "").Split('\n')
            .Select(x => x.Trim().TrimStart('*').TrimStart())
            .FirstOrDefault(x => x.StartsWith(distribution, StringComparison.Ordinal) &&
                x.Length > distribution.Length && char.IsWhiteSpace(x[distribution.Length]));
        var version = distributionLine?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (version != "2")
            return new(distribution, bashrc, "", "", false, false,
                "The selected distribution must use WSL2. Check with wsl --list --verbose.");
        var script = $$"""
            if [ ! -f {{WslCommands.Quote(bashrc)}} ]; then echo 'OpenFOAM bashrc was not found.' >&2; exit 2; fi
            source {{WslCommands.Quote(bashrc)}} || exit $?
            printf 'VERSION=%s\n' "$WM_PROJECT_VERSION"
            printf 'TUTORIAL=%s\n' "$FOAM_TUTORIALS/incompressibleFluid/cavity"
            command -v paraview >/dev/null && echo 'PARAVIEW=yes'
            [ "$WM_PROJECT_VERSION" = 13 ] || { echo 'This prototype supports Foundation 13 only.' >&2; exit 3; }
            for tool in blockMesh checkMesh foamRun foamDictionary setsid; do
                command -v "$tool" >/dev/null || { echo "Missing command: $tool" >&2; exit 4; }
            done
            test -f "$FOAM_TUTORIALS/incompressibleFluid/cavity/system/controlDict" || exit 5
            """;
        var result = await runner.RunAsync(WslCommands.Bash(distribution, script), cancellationToken: token);
        string Value(string key) => result.StandardOutput.Split('\n')
            .FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..].Trim() ?? "";
        return new(distribution, bashrc, Value("VERSION"), Value("TUTORIAL"), Value("PARAVIEW") == "yes",
            result.ExitCode == 0, (result.StandardError + result.StandardOutput).Trim());
    }

    public async Task<SimulationResult> RunCavityAsync(OpenFoamEnvironment environment,
        Action<LogLine> output, CancellationToken token = default)
    {
        if (!environment.Ready || environment.Version != "13")
            throw new InvalidOperationException("Check a supported OpenFOAM 13 environment first.");
        var runId = Guid.NewGuid().ToString("N");
        var work = $$"""
            source {{WslCommands.Quote(environment.Bashrc)}} || exit $?
            set -eo pipefail
            case_dir="$HOME/.local/share/FoamStudio/runs/{{runId}}/cavity"
            mkdir -p "$(dirname "$case_dir")"
            cp -R "$FOAM_TUTORIALS/incompressibleFluid/cavity" "$case_dir"
            cd "$case_dir"
            printf 'FOAMSTUDIO_CASE=%s\n' "$case_dir"
            exec > >(tee -a run.log) 2>&1
            echo '=== Mesh generation ==='
            blockMesh
            echo '=== Mesh validation ==='
            checkMesh | tee mesh-check.log
            grep -q 'Mesh OK.' mesh-check.log || { echo 'Mesh checks did not pass.'; exit 6; }
            echo '=== Cavity simulation (tutorial defaults) ==='
            foamRun
            touch cavity.foam
            echo '=== Simulation finished ==='
            """;
        string? casePath = null;
        var result = await RunJobAsync(environment.Distribution, work, line =>
        {
            if (line.Text.StartsWith("FOAMSTUDIO_CASE=", StringComparison.Ordinal))
                casePath = line.Text["FOAMSTUDIO_CASE=".Length..];
            output(line);
        }, token);
        return new(result.ExitCode, result.Cancelled, casePath);
    }

    // Linux processes have their own session: stopping wsl.exe alone does not stop a solver.
    // Keep the Windows host alive until the Linux process group has been stopped.
    public async Task<(int ExitCode, bool Cancelled)> RunJobAsync(string distribution,
        string script, Action<LogLine> output, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var pidReady = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = "printf 'FOAMSTUDIO_PID=%s\\n' \"$$\"\n" + script.Replace("\r\n", "\n");
        var command = WslCommands.Bash(distribution,
            "exec setsid --wait bash --noprofile --norc -c " + WslCommands.Quote(worker));
        var run = runner.RunAsync(command, line =>
        {
            if (line.Text.StartsWith("FOAMSTUDIO_PID=", StringComparison.Ordinal) &&
                int.TryParse(line.Text["FOAMSTUDIO_PID=".Length..], NumberStyles.None,
                    CultureInfo.InvariantCulture, out var pid) && pid > 1)
                pidReady.TrySetResult(pid);
            else output(line);
        });
        var cancelled = Task.Delay(Timeout.Infinite, token);
        if (await Task.WhenAny(run, cancelled) == run)
            return ((await run).ExitCode, false);

        // Cancellation can arrive before WSL has started. Wait for either its handshake or exit.
        if (await Task.WhenAny(run, pidReady.Task) == run)
            return ((await run).ExitCode, true);
        var processGroup = await pidReady.Task;
        output(new("Stopping the Linux job..."));
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stop = await runner.RunAsync(WslCommands.Bash(distribution, $$"""
            kill -TERM -- -{{processGroup}} 2>/dev/null || true
            for i in 1 2 3 4 5 6 7 8 9 10; do
                kill -0 -- -{{processGroup}} 2>/dev/null || exit 0
                sleep 0.2
            done
            kill -KILL -- -{{processGroup}} 2>/dev/null || true
            """), cancellationToken: stopTimeout.Token);
        if (stop.ExitCode != 0) throw new InvalidOperationException("Linux job cancellation failed; check the WSL job before closing.");
        try { return ((await run.WaitAsync(TimeSpan.FromSeconds(15))).ExitCode, true); }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException("The Linux stop request completed, but the job did not exit. Check the WSL job before running again.", ex);
        }
    }
}
