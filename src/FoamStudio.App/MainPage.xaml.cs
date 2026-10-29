using System.Collections.Concurrent;
using FoamStudio.Core;
using FoamStudio.Infrastructure;
using FoamStudio.OpenFoam;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FoamStudio.App;

public sealed partial class MainPage : Page
{
    private readonly OpenFoamService service = new(new ProcessRunner());
    private readonly ConcurrentQueue<string> pending = new();
    private readonly DispatcherTimer logTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private OpenFoamEnvironment? environment;
    private CancellationTokenSource? cancellation;
    private Task? operation;

    public MainPage()
    {
        InitializeComponent();
        Bashrc.Text = OpenFoamService.DefaultBashrc;
        logTimer.Tick += (_, _) => FlushLog();
        logTimer.Start();
        Loaded += async (_, _) => await ExecuteAsync(RefreshAsync);
    }

    private void Write(LogLine line) => pending.Enqueue((line.IsError ? "[stderr] " : "") + line.Text);

    private void FlushLog()
    {
        var lines = new List<string>();
        while (pending.TryDequeue(out var line)) lines.Add(line);
        if (lines.Count == 0) return;
        var text = Log.Text + string.Join(Environment.NewLine, lines) + Environment.NewLine;
        Log.Text = text.Length > 120_000 ? text[^120_000..] : text;
        LogScroll.ChangeView(null, LogScroll.ScrollableHeight, null);
    }

    private void SetStatus(string text, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        Status.Title = text;
        Status.Severity = severity;
    }

    private async Task ExecuteAsync(Func<CancellationToken, Task> action)
    {
        if (cancellation is not null) return;
        using var source = new CancellationTokenSource();
        cancellation = source;
        SetBusy(true);
        try { operation = action(source.Token); await operation; }
        catch (OperationCanceledException) { SetStatus("Operation cancelled."); }
        catch (Exception ex) { Write(new(ex.Message, true)); SetStatus(ex.Message, InfoBarSeverity.Error); }
        finally
        {
            operation = null;
            cancellation = null;
            FlushLog();
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        Busy.IsActive = busy;
        Refresh.IsEnabled = Check.IsEnabled = Distribution.IsEnabled = Bashrc.IsEnabled = !busy;
        Run.IsEnabled = !busy && environment?.Ready == true;
        Stop.IsEnabled = busy;
    }

    private async Task RefreshAsync(CancellationToken token)
    {
        environment = null;
        SetStatus("Looking for WSL distributions...");
        var distributions = await service.ListDistributionsAsync(token);
        Distribution.ItemsSource = distributions;
        Distribution.SelectedItem = distributions.FirstOrDefault(x => x == "Ubuntu-22.04") ?? distributions.FirstOrDefault();
        SetStatus(distributions.Length == 0 ? "No WSL distributions found. Install WSL2 and Ubuntu first." : "Select a distribution and check its environment.");
    }

    private async void RefreshClicked(object sender, RoutedEventArgs e) => await ExecuteAsync(RefreshAsync);
    private async void CheckClicked(object sender, RoutedEventArgs e) => await ExecuteAsync(async token =>
    {
        environment = null;
        if (Distribution.SelectedItem is not string name) throw new InvalidOperationException("Select a WSL distribution.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        var found = await service.ProbeAsync(name, Bashrc.Text.Trim(), timeout.Token);
        environment = found;
        Write(new(found.Details));
        SetStatus(found.Ready ? $"Ready: OpenFOAM {found.Version} in {found.Distribution}. ParaView: {(found.HasParaView ? "available" : "not found")}."
            : "Environment is not ready. See the diagnostic output.", found.Ready ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
    });

    private async void RunClicked(object sender, RoutedEventArgs e) => await ExecuteAsync(async token =>
    {
        if (environment is not { Ready: true } ready) return;
        Log.Text = "";
        SetStatus("Running cavity — generating mesh, checking mesh, then solving.");
        CaseLocation.Text = "Preparing a new case...";
        var result = await service.RunCavityAsync(ready, Write, token);
        CaseLocation.Text = result.CasePath is { } path ? $"{ready.Distribution}: {path} (run.log)" : "No case was created.";
        SetStatus(result.Cancelled ? "Simulation stopped. Partial results and logs were retained."
            : result.ExitCode == 0 ? "Simulation finished successfully."
            : $"Simulation failed (exit {result.ExitCode}). See the log.",
            result.Cancelled ? InfoBarSeverity.Warning : result.ExitCode == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Error);
    });

    private void StopClicked(object sender, RoutedEventArgs e)
    {
        cancellation?.Cancel();
        Stop.IsEnabled = false;
        SetStatus("Stopping the current operation...");
    }

    private void EnvironmentChanged(object sender, SelectionChangedEventArgs e) => InvalidateEnvironment();
    private void EnvironmentTextChanged(object sender, TextChangedEventArgs e) => InvalidateEnvironment();
    private void InvalidateEnvironment()
    {
        environment = null;
        if (Run is not null) Run.IsEnabled = false;
    }

    public async Task<bool> StopAndWaitAsync()
    {
        cancellation?.Cancel();
        try { if (operation is not null) await operation; }
        catch (OperationCanceledException) { }
        catch (Exception ex) { SetStatus(ex.Message, InfoBarSeverity.Error); return false; }
        logTimer.Stop();
        return true;
    }
}
