using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MGrep;

/// <summary>
/// Evaluates whether a line of text matches the user-configured search criteria,
/// and locates the exact character spans of each match for highlight rendering.
/// </summary>
/// <remarks>
/// Supports three orthogonal modes:
/// <list type="bullet">
///   <item><description>Plain substring (default)</description></item>
///   <item><description>Whole-word — matches only at word boundaries</description></item>
///   <item><description>Regular expression — delegates to <see cref="Regex"/></description></item>
/// </list>
/// Case sensitivity is controlled independently of the mode.
/// </remarks>
public sealed class Filter
{
    private readonly bool matchCase;
    private readonly bool matchWholeWord;
    private readonly bool useRegex;
    private readonly string pattern;

    // Pre-compiled regex when useRegex is true; null otherwise.
    private readonly Regex? regex;

    /// <param name="matchCase">When <see langword="true"/>, the comparison is case-sensitive.</param>
    /// <param name="matchWholeWord">
    ///   When <see langword="true"/>, the pattern must be surrounded by non-word characters.
    /// </param>
    /// <param name="useRegex">
    ///   When <see langword="true"/>, <paramref name="pattern"/> is treated as a regular expression.
    /// </param>
    /// <param name="pattern">The search pattern or regex string.</param>
    public Filter(bool matchCase, bool matchWholeWord, bool useRegex, string pattern)
    {
        this.matchCase = matchCase;
        this.matchWholeWord = matchWholeWord;
        this.useRegex = useRegex;
        this.pattern = pattern;

        if (useRegex)
        {
            var regexPattern = matchWholeWord ? $@"\b{pattern}\b" : pattern;
            var regexOptions = RegexOptions.Compiled;
            if (!matchCase)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            regex = new Regex(regexPattern, regexOptions);
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="line"/> contains at least one match.
    /// </summary>
    public bool IsMatch(string line)
    {
        if (useRegex)
        {
            return regex!.IsMatch(line);
        }

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (matchWholeWord)
        {
            var start = 0;
            var match = false;
            do
            {
                var index = line.IndexOf(pattern, start, comparison);
                match = index != -1 &&
                            (index == 0 || !IsWordCharacter(line[index - 1])) &&
                            (index + pattern.Length == line.Length || !IsWordCharacter(line[index + pattern.Length]));
                start = index + 1;
            }
            while (!match && start > 0);

            return match;
        }

        return line.Contains(pattern, comparison);
    }

    /// <summary>
    /// Returns the start index and length of every match within <paramref name="line"/>,
    /// preserving the same case, whole-word, and regex options used during the search.
    /// Used by <see cref="Behaviors.TextHighlighter"/> to highlight matched text.
    /// </summary>
    public (int Start, int Length)[] GetMatchSpans(string line)
    {
        if (useRegex)
        {
            return regex!.Matches(line)
                         .Select(m => (m.Index, m.Length))
                         .ToArray();
        }

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var spans = new List<(int Start, int Length)>();
        var pos = 0;
        while (pos < line.Length)
        {
            var index = line.IndexOf(pattern, pos, comparison);
            if (index == -1)
            {
                break;
            }

            if (!matchWholeWord ||
                ((index == 0 || !IsWordCharacter(line[index - 1])) &&
                 (index + pattern.Length == line.Length || !IsWordCharacter(line[index + pattern.Length]))))
            {
                spans.Add((index, pattern.Length));
            }

            pos = index + 1;
        }

        return spans.ToArray();
    }

    // A word character is any letter, digit, or underscore (mirrors \w in regex).
    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
}
