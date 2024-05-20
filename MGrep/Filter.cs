using System;
using System.Text.RegularExpressions;

namespace MGrep;

public sealed class Filter
{
    private readonly bool matchCase;
    private readonly bool matchWholeWord;
    private readonly bool useRegex;
    private readonly string pattern;

    private readonly Regex? regex;

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
            } while (!match && start > 0);

            return match;
        }

        return line.Contains(pattern, comparison);
    }

    static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
}