using System.Windows;

namespace MGrep;

public partial class App
{
    private Window? mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        var searchOptions= new Options<SearchOptions>("Search", "MGrep.config");
        var mainWindowViewModel = new MainWindowViewModel(searchOptions);
        var windowOptions = new Options<WindowOptions>("Window", "MGrep.config");
        mainWindow = new MainWindow(mainWindowViewModel, windowOptions);
        mainWindow.Show();

        base.OnStartup(e);
    }
}
