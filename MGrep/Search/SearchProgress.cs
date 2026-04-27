using System;

namespace MGrep;

/// <summary>The lifecycle state of a search operation.</summary>
public enum SearchState
{
    /// <summary>The search is currently running.</summary>
    Searching,

    /// <summary>The search finished normally.</summary>
    Completed,

    /// <summary>The search was cancelled by the user.</summary>
    Cancelled,

    /// <summary>The search terminated due to an unhandled error.</summary>
    Faulted
}

/// <summary>
/// Immutable snapshot of search progress reported approximately once per second
/// and on final completion.
/// </summary>
/// <param name="FileCount">Total number of files examined so far.</param>
/// <param name="FileIgnoreCount">Files skipped because they appeared to be binary.</param>
/// <param name="FileMatchCount">Files that contained at least one match.</param>
/// <param name="ErrorCount">Files that could not be read (e.g. permission denied).</param>
/// <param name="MatchCount">Total number of individual line matches found.</param>
/// <param name="State">Current lifecycle state of the search.</param>
/// <param name="Elapsed">Time elapsed since the search started.</param>
public readonly record struct SearchProgress(
    int FileCount,
    int FileIgnoreCount,
    int FileMatchCount,
    int ErrorCount,
    int MatchCount,
    SearchState State,
    TimeSpan Elapsed);
