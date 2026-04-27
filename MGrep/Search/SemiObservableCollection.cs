using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MGrep;

/// <summary>
/// A sorted, append-only list of <see cref="Match"/> items that raises
/// <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/> so
/// a bound <see cref="System.Windows.Controls.ListView"/> updates incrementally as
/// search results arrive.
/// </summary>
/// <remarks>
/// Items are always kept sorted by file name then line number.
/// <see cref="AddRange"/> relies on this invariant — new batches must also be pre-sorted.
/// </remarks>
public class SemiObservableCollection : IReadOnlyList<Match>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly List<Match> matches = new();
    private readonly Comparer<Match> comparer = Comparer<Match>.Create((left, right) =>
    {
        var comparison = string.Compare(left.FileName, right.FileName, StringComparison.Ordinal);
        return comparison == 0 ? left.LineNumber.CompareTo(right.LineNumber) : comparison;
    });

    /// <inheritdoc/>
    public int Count => matches.Count;

    /// <inheritdoc/>
    public Match this[int index] => matches[index];

    /// <summary>
    /// Inserts <paramref name="moreMatches"/> into the sorted list at the correct position
    /// and raises a single <see cref="NotifyCollectionChangedAction.Add"/> notification.
    /// </summary>
    /// <remarks>
    /// Assumption: <paramref name="moreMatches"/> is already sorted by file name and line number,
    /// matching the collection's own sort order.
    /// </remarks>
    public void AddRange(List<Match> moreMatches)
    {
        if (moreMatches.Count == 0)
        {
            return;
        }

        var startingIndex = ~matches.BinarySearch(moreMatches[0], comparer);
        matches.InsertRange(startingIndex, moreMatches);

        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, moreMatches, startingIndex));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    /// <summary>Removes all items and raises a <see cref="NotifyCollectionChangedAction.Reset"/> notification.</summary>
    public void Clear()
    {
        matches.Clear();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    /// <inheritdoc/>
    public IEnumerator<Match> GetEnumerator() => matches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
