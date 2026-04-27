using System.Collections.Generic;

namespace MGrep;

/// <summary>
/// Persisted search preferences and input history.
/// Stored in the "Search" section of MGrep.config.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>Most-recently-used folder paths, newest first (max 10).</summary>
    public List<string> FolderHistory { get; set; } = new();

    /// <summary>Most-recently-used search patterns, newest first (max 10).</summary>
    public List<string> PatternHistory { get; set; } = new();

    /// <summary>Most-recently-used file-pattern strings, newest first (max 10).</summary>
    public List<string> FilePatternsHistory { get; set; } = new();

    /// <summary>
    /// <see langword="true"/> when the last session used a file-pattern filter,
    /// so it can be restored on next launch.
    /// </summary>
    public bool UsingFilePatterns { get; set; }

    /// <summary>When <see langword="true"/>, the pattern comparison is case-sensitive.</summary>
    public bool MatchCase { get; set; }

    /// <summary>When <see langword="true"/>, the pattern must match a complete word.</summary>
    public bool MatchWholeWord { get; set; }

    /// <summary>When <see langword="true"/>, the pattern is interpreted as a regular expression.</summary>
    public bool UseRegex { get; set; }

    /// <summary>
    /// When <see langword="true"/>, file patterns use full glob syntax (path separators allowed).
    /// Mutually exclusive with <see cref="IncludeSubfolders"/>.
    /// </summary>
    public bool Globbing { get; set; }

    /// <summary>
    /// When <see langword="true"/>, the search recurses into sub-directories.
    /// Mutually exclusive with <see cref="Globbing"/>.
    /// </summary>
    public bool IncludeSubfolders { get; set; }

    /// <summary>When <see langword="true"/>, binary files are searched as well as text files.</summary>
    public bool IncludeBinaryFiles { get; set; }
}
