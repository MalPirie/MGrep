using Shouldly;
using System.IO.Abstractions.TestingHelpers;

namespace MGrep.Test;

public class ThemeViewModelTests
{
    private static (MainWindowViewModel vm, MockThemeService ts) CreateViewModel(AppTheme initialTheme = AppTheme.Light)
    {
        var themeService = new MockThemeService();
        themeService.Apply(initialTheme);
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var vm = new MainWindowViewModel(options, new MockDialogService(), fileSystem, themeService);
        return (vm, themeService);
    }

    [Fact]
    public void WhenInitialisedWithLightThemeThenIsDarkModeShouldBeFalse()
    {
        var (vm, _) = CreateViewModel(AppTheme.Light);

        vm.IsDarkMode.ShouldBeFalse();
    }

    [Fact]
    public void WhenInitialisedWithDarkThemeThenIsDarkModeShouldBeTrue()
    {
        var (vm, _) = CreateViewModel(AppTheme.Dark);

        vm.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public void WhenToggleThemeCommandExecutedThenThemeToggles()
    {
        var (vm, ts) = CreateViewModel(AppTheme.Light);

        vm.ToggleThemeCommand.Execute(null);

        ts.CurrentTheme.ShouldBe(AppTheme.Dark);
        vm.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public void WhenThemeChangesExternallyThenIsDarkModeUpdates()
    {
        var (vm, ts) = CreateViewModel(AppTheme.Light);

        ts.Apply(AppTheme.Dark);

        vm.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public void WhenThemeServiceIsNullThenIsDarkModeDefaultsToFalse()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var vm = new MainWindowViewModel(options);

        vm.IsDarkMode.ShouldBeFalse();
    }

    [Fact]
    public void WhenTogglingTwiceThenThemeReturnToOriginal()
    {
        var (vm, ts) = CreateViewModel(AppTheme.Light);

        vm.ToggleThemeCommand.Execute(null);
        vm.ToggleThemeCommand.Execute(null);

        ts.CurrentTheme.ShouldBe(AppTheme.Light);
        vm.IsDarkMode.ShouldBeFalse();
    }
}
