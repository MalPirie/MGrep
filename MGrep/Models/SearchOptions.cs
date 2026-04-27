using System.Collections.Generic;

namespace MGrep;

public sealed class SearchOptions
{
    public List<string> FolderHistory { get; set; } = new();
    public List<string> PatternHistory { get; set; } = new();
    public List<string> FilePatternsHistory { get; set; } = new();
    public bool UsingFilePatterns { get; set; }
    public bool MatchCase { get; set; }
    public bool MatchWholeWord { get; set; }
    public bool UseRegex { get; set; }
    public bool Globbing { get; set; }
    public bool IncludeSubfolders { get; set; }
    public bool IncludeBinaryFiles { get; set; }
}