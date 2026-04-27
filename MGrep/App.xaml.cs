using System.IO.Abstractions;
using System.Windows;

namespace MGrep;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Base class must be called first so WPF initialises the application state
        // before any windows or services are created.
        base.OnStartup(e);

        // Initialise the theme BEFORE creating any windows to prevent a flash-of-wrong-theme
        // on startup (the window background would briefly show the default theme colour).
        var themeOptions = new Options<ThemeOptions>("Theme", "MGrep.config");
        var themeService = new ThemeService(themeOptions);
        themeService.Initialize();

        // "Search" section — persists search inputs, toggles, and history lists.
        var searchOptions = new Options<SearchOptions>("Search", "MGrep.config");
        var mainWindowViewModel = new MainWindowViewModel(
            searchOptions, new DialogService(), new FileSystem(), themeService);

        // "Window" section — persists position, size, and maximised state.
        var windowOptions = new Options<WindowOptions>("Window", "MGrep.config");
        var mainWindow = new MainWindow(mainWindowViewModel, windowOptions);
        mainWindow.Show();
    }
}
