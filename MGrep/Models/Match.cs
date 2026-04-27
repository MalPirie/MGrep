using System.IO;

namespace MGrep;

/// <summary>
/// Represents a single line in a file that matched the search pattern.
/// </summary>
/// <param name="FileName">Absolute path to the file containing the match.</param>
/// <param name="LineNumber">1-based line number within the file.</param>
/// <param name="Text">The full text of the matching line (not trimmed).</param>
public readonly record struct Match(string FileName, int LineNumber, string Text)
{
    /// <summary>Gets the file name without directory path (for display in the results list).</summary>
    public string Name => Path.GetFileName(FileName);

    /// <summary>
    /// The character spans within <see cref="Text"/> that matched the search pattern.
    /// Populated by <see cref="Searcher"/> during the search; used by
    /// <see cref="Behaviors.TextHighlighter"/> to highlight matched text in the UI.
    /// </summary>
    public (int Start, int Length)[] Spans { get; init; } = [];
}
