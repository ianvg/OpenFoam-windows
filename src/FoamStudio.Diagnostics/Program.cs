using FoamStudio.Core;
using FoamStudio.Infrastructure;
using FoamStudio.OpenFoam;

var service = new OpenFoamService(new ProcessRunner());
var distribution = args.Skip(1).FirstOrDefault() ?? "Ubuntu-22.04";
var mode = args.FirstOrDefault() ?? "probe";
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
void Log(LogLine line) => Console.WriteLine((line.IsError ? "[stderr] " : "") + line.Text);
try
{
    Console.WriteLine("WSL distributions: " + string.Join(", ", await service.ListDistributionsAsync(cancellation.Token)));
    if (mode == "cancel-test")
    {
        var marker = "foamstudio-cancel-" + Guid.NewGuid().ToString("N");
        // Include a child that ignores TERM: verify the KILL fallback as well as the parent.
        var result = await service.RunJobAsync(distribution,
            "bash -c " + WslCommands.Quote("trap '' TERM; echo CHILD_READY; exec -a " + marker + " sleep 120") + " &\nwait",
            line => { Log(line); if (line.Text == "CHILD_READY") cancellation.Cancel(); }, cancellation.Token);
        var check = await new ProcessRunner().RunAsync(WslCommands.Bash(distribution,
            "pgrep -f -- " + WslCommands.Quote("^" + marker + " ")));
        if (!result.Cancelled || check.ExitCode != 1)
            throw new InvalidOperationException("Cancellation failed: a Linux child may still be running.");
        Console.WriteLine("PASS: cancellation removed the Linux job and its TERM-resistant child.");
        return 0;
    }
    var environment = await service.ProbeAsync(distribution, OpenFoamService.DefaultBashrc, cancellation.Token);
    Console.WriteLine(environment.Details);
    if (!environment.Ready) return 2;
    if (mode == "probe") return 0;
    if (mode != "cavity") throw new ArgumentException("Use probe, cavity, or cancel-test [distribution].");
    var simulation = await service.RunCavityAsync(environment, Log, cancellation.Token);
    Console.WriteLine($"Case: {simulation.CasePath}; exit: {simulation.ExitCode}; cancelled: {simulation.Cancelled}");
    return simulation.Cancelled ? 130 : simulation.ExitCode;
}
catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
