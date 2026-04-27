using Microsoft.Win32;
using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace MGrep;

/// <summary>
/// Implements runtime Light/Dark theme switching.
/// <list type="bullet">
///   <item>Delegates WPF-UI internal token updates to <see cref="ApplicationThemeManager"/>.</item>
///   <item>Swaps the app-level semantic theme dictionary (Light.xaml / Dark.xaml).</item>
///   <item>Persists the user's choice via <see cref="Options{T}"/>.</item>
///   <item>Follows the OS theme on first launch; the user override persists thereafter.</item>
/// </list>
/// </summary>
public sealed class ThemeService : IThemeService
{
    private static readonly Uri LightThemeUri =
        new("pack://application:,,,/Themes/Light.xaml", UriKind.Absolute);

    private static readonly Uri DarkThemeUri =
        new("pack://application:,,,/Themes/Dark.xaml", UriKind.Absolute);

    private readonly Options<ThemeOptions> options;
    private bool initialized;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;
    public bool IsDarkMode => CurrentTheme == AppTheme.Dark;

    public event EventHandler<AppTheme>? ThemeChanged;

    public ThemeService(Options<ThemeOptions> options)
    {
        this.options = options;
    }

    /// <summary>
    /// Determines and applies the initial theme. Call this before showing any window.
    /// </summary>
    public void Initialize()
    {
        var saved = options.Value.Theme;
        var theme = saved switch
        {
            nameof(AppTheme.Dark)  => AppTheme.Dark,
            nameof(AppTheme.Light) => AppTheme.Light,
            _                      => GetOsTheme()   // null / empty = follow OS
        };
        Apply(theme);
    }

    public void Apply(AppTheme theme)
    {
        // Short-circuit if already applied and not the first call (avoids duplicate config writes)
        if (initialized && CurrentTheme == theme)
        {
            return;
        }

        initialized = true;
        CurrentTheme = theme;

        // 1. Update WPF-UI's internal theme tokens.
        //    - WindowBackdropType.None: no Mica/Acrylic — not supported in WPF.
        //    - updateAccent: false — preserve Milliman brand accent colours;
        //      do NOT override with the user's Windows system accent colour.
        ApplicationThemeManager.Apply(
            theme == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
            WindowBackdropType.None,
            updateAccent: false);

        // 2. Swap the semantic brush dictionary (Light.xaml / Dark.xaml).
        SwapThemeDictionary(theme);

        // 3. Persist user preference.
        options.Update(o => o.Theme = theme.ToString());

        ThemeChanged?.Invoke(this, theme);
    }

    public void Toggle() => Apply(IsDarkMode ? AppTheme.Light : AppTheme.Dark);

    private static AppTheme GetOsTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            // AppsUseLightTheme == 0 means dark mode is active
            return key?.GetValue("AppsUseLightTheme") is 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch
        {
            return AppTheme.Light;
        }
    }

    private static void SwapThemeDictionary(AppTheme theme)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var newUri = theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri;

        // Locate the currently loaded semantic theme dictionary
        var insertIndex = -1;
        ResourceDictionary? existing = null;

        for (var i = 0; i < dicts.Count; i++)
        {
            var source = dicts[i].Source?.OriginalString ?? string.Empty;
            if (source.EndsWith("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Themes/Dark.xaml",  StringComparison.OrdinalIgnoreCase))
            {
                existing = dicts[i];
                insertIndex = i;
                break;
            }
        }

        if (existing != null)
        {
            // Replace at the same position to preserve merge order
            dicts.Remove(existing);
            dicts.Insert(insertIndex, new ResourceDictionary { Source = newUri });
        }
        else
        {
            // Fallback: append (merge order may differ from ideal — investigate if this fires)
            dicts.Add(new ResourceDictionary { Source = newUri });
        }
    }
}
