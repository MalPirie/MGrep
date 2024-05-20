using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MGrep;

public class SemiObservableCollection : IReadOnlyList<Match>, INotifyCollectionChanged, INotifyPropertyChanged
{
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly List<Match> matches = new();
    private readonly Comparer<Match> comparer = Comparer<Match>.Create((left, right) =>
    {
        var comparison = string.Compare(left.FileName, right.FileName, StringComparison.Ordinal);
        return comparison == 0 ? left.LineNumber.CompareTo(right.LineNumber) : comparison;
    });

    public int Count => matches.Count;

    public Match this[int index] => matches[index];

    //Assumption: The list of matches is always sorted by file name and line number
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

    public void Clear()
    {
        matches.Clear();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    public IEnumerator<Match> GetEnumerator() => matches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

