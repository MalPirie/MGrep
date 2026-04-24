using System;

namespace MGrep;

/// <summary>
/// Represents the application display theme.
/// </summary>
public enum AppTheme
{
    Light,
    Dark
}

/// <summary>
/// Manages runtime light/dark theme switching.
/// All theme logic must go through this service — no code-behind theming.
/// </summary>
public interface IThemeService
{
    /// <summary>Gets the currently active theme.</summary>
    AppTheme CurrentTheme { get; }

    /// <summary>Gets a value indicating whether dark mode is active.</summary>
    bool IsDarkMode { get; }

    /// <summary>Applies the specified theme immediately.</summary>
    void Apply(AppTheme theme);

    /// <summary>Toggles between Light and Dark.</summary>
    void Toggle();

    /// <summary>Fired after the theme changes. Argument is the new theme.</summary>
    event EventHandler<AppTheme>? ThemeChanged;
}
