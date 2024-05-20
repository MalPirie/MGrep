using System.Windows;

namespace MGrep;

public partial class App
{
    private Window? mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        var searchOptions= new Options<SearchOptions>("Search", "application.settings");
        var mainWindowViewModel = new MainWindowViewModel(searchOptions);
        var windowOptions = new Options<WindowOptions>("Window", "application.settings");
        mainWindow = new MainWindow(mainWindowViewModel, windowOptions);
        mainWindow.Show();

        base.OnStartup(e);
    }
}
