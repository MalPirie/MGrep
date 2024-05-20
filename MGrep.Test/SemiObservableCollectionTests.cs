using System.Collections.Specialized;
using System.ComponentModel;
using Shouldly;

namespace MGrep.Test;

public class SemiObservableCollectionTests 
{
    private readonly List<object> argsList = [];

    [Fact]
    public void WhenClearingCollectionThenRaisesResetEvent()
    {
        var collection = new SemiObservableCollection();
        collection.CollectionChanged += CollectionChangedHandler;
        collection.PropertyChanged += PropertyChangedHandler;

        collection.Clear();

        argsList.Count.ShouldBe(2);
        argsList.OfType<NotifyCollectionChangedEventArgs>().ShouldContain(item =>
            item.Action == NotifyCollectionChangedAction.Reset);
        argsList.OfType<PropertyChangedEventArgs>().ShouldContain(item =>
            item.PropertyName == nameof(SemiObservableCollection.Count));

        collection.CollectionChanged -= CollectionChangedHandler;
        collection.PropertyChanged -= PropertyChangedHandler;
    }

    [Fact]
    public void WhenAddingEmptyRangeThenNoEventsAreRaised()
    {
        var collection = new SemiObservableCollection();
        collection.CollectionChanged += CollectionChangedHandler;
        collection.PropertyChanged += PropertyChangedHandler;

        collection.AddRange([]);

        argsList.ShouldBeEmpty();

        collection.CollectionChanged -= CollectionChangedHandler;
        collection.PropertyChanged -= PropertyChangedHandler;
    }

    [Fact]
    public void WhenAddingRangeThenRaisesAddEvent()
    {
        var collection = new SemiObservableCollection();
        collection.AddRange([
            new Match("file1", 1, "Line 1"),
            new Match("file1", 2, "Line 2"),
        ]);
        collection.CollectionChanged += CollectionChangedHandler;
        collection.PropertyChanged += PropertyChangedHandler;

        collection.AddRange([
            new Match("file2", 1, "Line 1"),
            new Match("file2", 2, "Line 2"),
            new Match("file2", 3, "Line 3"),
            new Match("file2", 4, "Line 4")
        ]);

        argsList.Count.ShouldBe(2);
        argsList.OfType<NotifyCollectionChangedEventArgs>().ShouldContain(item => 
            item.Action == NotifyCollectionChangedAction.Add && item.NewItems != null && item.NewItems.Count == 4 &&item.NewStartingIndex == 2);
        argsList.OfType<PropertyChangedEventArgs>().ShouldContain(item =>
            item.PropertyName == nameof(SemiObservableCollection.Count));

        collection.CollectionChanged -= CollectionChangedHandler;
        collection.PropertyChanged -= PropertyChangedHandler;
    }

    private void CollectionChangedHandler(object? sender, NotifyCollectionChangedEventArgs args) => argsList.Add(args);

    private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs args) => argsList.Add(args);
}
