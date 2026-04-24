namespace MGrep;

/// <summary>
/// Persisted user theme preferences.
/// Stored in the "Theme" section of MGrep.config.
/// </summary>
public sealed class ThemeOptions
{
    /// <summary>
    /// "Light", "Dark", or null / empty to follow the OS setting on next launch.
    /// </summary>
    public string? Theme { get; set; }
}
