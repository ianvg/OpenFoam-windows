using Microsoft.UI.Xaml;

namespace FoamStudio.App;

public partial class App : Application
{
    private Window? window;
    private bool closing;
    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
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
