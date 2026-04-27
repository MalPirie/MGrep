using System;

namespace MGrep;

public enum SearchState
{
    Searching,
    Completed,
    Cancelled,
    Faulted
}

public readonly record struct SearchProgress(int FileCount, int FileIgnoreCount, int FileMatchCount, int ErrorCount, int MatchCount, SearchState State, TimeSpan Elapsed);