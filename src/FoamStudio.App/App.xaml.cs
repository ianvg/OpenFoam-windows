using Microsoft.UI.Xaml;

namespace FoamStudio.App;

public partial class App : Application
{
    // Setup checks existence; retain this handle for the lifetime of every instance.
    private static readonly Mutex runningMutex = new(false, "Local\\FoamStudio.Running");
    private Window? window;
    private bool closing;
    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        GC.KeepAlive(runningMutex);
        var page = new MainPage();
        window = new Window { Title = "Foam Studio — OpenFOAM prototype", Content = page };
        window.AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 800));
        window.AppWindow.Closing += async (_, e) =>
        {
            if (closing) return;
            e.Cancel = true;
            if (await page.StopAndWaitAsync())
            {
                closing = true;
                window.Close();
            }
        };
        window.Activate();
    }
}
