namespace MGrep.Test;

public sealed class MockThemeService : IThemeService
{
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;
    public bool IsDarkMode => CurrentTheme == AppTheme.Dark;
    public int ToggleCallCount { get; private set; }

    public event EventHandler<AppTheme>? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;
        ThemeChanged?.Invoke(this, theme);
    }

    public void Toggle()
    {
        ToggleCallCount++;
        Apply(IsDarkMode ? AppTheme.Light : AppTheme.Dark);
    }
}
