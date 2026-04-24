using System.IO.Abstractions;
using System.Linq;
using System.Windows;

namespace MGrep;

public partial class App
{
    private Window? mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialise theme BEFORE creating any windows to prevent flash-of-wrong-theme
        var themeOptions = new Options<ThemeOptions>("Theme", "MGrep.config");
        var themeService = new ThemeService(themeOptions);
        themeService.Initialize();

        var searchOptions = new Options<SearchOptions>("Search", "MGrep.config");
        var mainWindowViewModel = new MainWindowViewModel(searchOptions, new DialogService(), new FileSystem(), themeService);
        var windowOptions = new Options<WindowOptions>("Window", "MGrep.config");
        mainWindow = new MainWindow(mainWindowViewModel, windowOptions);
        mainWindow.Show();

        base.OnStartup(e);
    }
}
