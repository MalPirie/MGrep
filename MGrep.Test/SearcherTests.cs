using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Shouldly;

namespace MGrep.Test;

// NOTE that the yield is required to ensure that the last progress event is handled before
// asserting on the results.
public class SearcherTests
{
    [Fact]
    public async Task WhenProcessingTextFilesThenReturnsMatchesAndProgress()
    {
        var fileSystem = MakeFileSystem();
        var fileFilter = new MockFileFilter([@"C:\Test\File1.txt", @"C:\Test\File11.txt", @"C:\Test\File21.txt"]);
        var filter = new Filter(false, false, false, "Line 1");
        var matches = new List<Match>();
        var progressEvents = new List<SearchProgress>();
        var progress = new Progress<SearchProgress>(progressEvents.Add);
        var searcher = new Searcher(false, fileFilter, filter, fileSystem);

        await foreach (var fileMatches in searcher.SearchAsync(progress, CancellationToken.None))
        {
            matches.AddRange(fileMatches);
        }

        await Task.Yield();

        matches.Count.ShouldBe(4);
        progressEvents.Last().ShouldSatisfyAllConditions(
            p => p.FileCount.ShouldBe(3),
            p => p.FileIgnoreCount.ShouldBe(1),
            p => p.FileMatchCount.ShouldBe(2),
            p => p.MatchCount.ShouldBe(4),
            p => p.State.ShouldBe(SearchState.Completed));
    }

    [Fact]
    public async Task WhenIncludingBinaryFileThenReturnsMatchesInBinaryFiles()
    {
        var fileSystem = MakeFileSystem();
        var fileFilter = new MockFileFilter([@"C:\Test\File1.txt", @"C:\Test\File11.txt", @"C:\Test\File21.txt"]);
        var filter = new Filter(false, false, false, "Line 1");
        var matches = new List<Match>();
        var progressEvents = new List<SearchProgress>();
        var progress = new Progress<SearchProgress>(progressEvents.Add);
        var searcher = new Searcher(true, fileFilter, filter, fileSystem);

        await foreach (var fileMatches in searcher.SearchAsync(progress, CancellationToken.None))
        {
            matches.AddRange(fileMatches);
        }

        await Task.Yield();

        matches.Count.ShouldBe(6);
        progressEvents.Last().ShouldSatisfyAllConditions(
            p => p.FileCount.ShouldBe(3),
            p => p.FileIgnoreCount.ShouldBe(0),
            p => p.FileMatchCount.ShouldBe(3),
            p => p.MatchCount.ShouldBe(6),
            p => p.State.ShouldBe(SearchState.Completed));
    }

    [Fact]
    public async Task WhenCancelled()
    {
        var fileSystem = MakeFileSystem();
        var fileFilter = new MockFileFilter([@"C:\Test\File1.txt", @"C:\Test\File11.txt", @"C:\Test\File21.txt"]);
        var filter = new Filter(false, false, false, "Line 1");
        var matches = new List<Match>();
        var progressEvents = new List<SearchProgress>();
        var progress = new Progress<SearchProgress>(progressEvents.Add);
        var searcher = new Searcher(false, fileFilter, filter, fileSystem);

        var source = new CancellationTokenSource();
        source.Cancel();
        ;
        await foreach (var fileMatches in searcher.SearchAsync(progress, source.Token))
        {
            matches.AddRange(fileMatches);
        }

        await Task.Yield();

        matches.Count.ShouldBe(0);
        progressEvents.Last().State.ShouldBe(SearchState.Cancelled);
    }

    [Fact]
    public void GetMatchSpans_PlainText_SingleMatch()
    {
        var searcher = MakeSearcher("hello");
        searcher.GetMatchSpans("say hello world").ShouldBe([(4, 5)]);
    }

    [Fact]
    public void GetMatchSpans_PlainText_MultipleMatches()
    {
        var searcher = MakeSearcher("ab");
        searcher.GetMatchSpans("ab cd ab").ShouldBe([(0, 2), (6, 2)]);
    }

    [Fact]
    public void GetMatchSpans_PlainText_NoMatch()
    {
        var searcher = MakeSearcher("xyz");
        searcher.GetMatchSpans("hello world").ShouldBeEmpty();
    }

    [Fact]
    public void GetMatchSpans_CaseInsensitive()
    {
        var searcher = MakeSearcher("hello", matchCase: false);
        searcher.GetMatchSpans("Hello HELLO hello").ShouldBe([(0, 5), (6, 5), (12, 5)]);
    }

    [Fact]
    public void GetMatchSpans_CaseSensitive_OnlyMatchesCorrectCase()
    {
        var searcher = MakeSearcher("hello", matchCase: true);
        searcher.GetMatchSpans("Hello HELLO hello").ShouldBe([(12, 5)]);
    }

    [Fact]
    public void GetMatchSpans_WholeWord_MatchesWordBoundaries()
    {
        var searcher = MakeSearcher("word", matchWholeWord: true);
        searcher.GetMatchSpans("a word in a crossword puzzle word").ShouldBe([(2, 4), (29, 4)]);
    }

    [Fact]
    public void GetMatchSpans_WholeWord_NoMatchWhenEmbedded()
    {
        var searcher = MakeSearcher("word", matchWholeWord: true);
        searcher.GetMatchSpans("crossword").ShouldBeEmpty();
    }

    [Fact]
    public void GetMatchSpans_Regex_SingleMatch()
    {
        var searcher = MakeSearcher(@"\d+", useRegex: true);
        searcher.GetMatchSpans("value 42 end").ShouldBe([(6, 2)]);
    }

    [Fact]
    public void GetMatchSpans_Regex_MultipleMatches()
    {
        var searcher = MakeSearcher(@"\d+", useRegex: true);
        searcher.GetMatchSpans("1 and 22 and 333").ShouldBe([(0, 1), (6, 2), (13, 3)]);
    }

    [Fact]
    public void GetMatchSpans_Regex_NoMatch()
    {
        var searcher = MakeSearcher(@"\d+", useRegex: true);
        searcher.GetMatchSpans("no digits here").ShouldBeEmpty();
    }

    private static Searcher MakeSearcher(
        string pattern,
        bool matchCase = false,
        bool matchWholeWord = false,
        bool useRegex = false)
    {
        var filter = new Filter(matchCase, matchWholeWord, useRegex, pattern);
        return new Searcher(false, new MockFileFilter([]), filter);
    }

    private MockFileSystem MakeFileSystem() =>
        new (new Dictionary<string, MockFileData>
        {
            { @"C:\Test\File1.txt", MakeFileData(i => $"File 1, Line {i}", 10) },
            { @"C:\Test\File2.txt", MakeFileData(i => $"File 2, Line {i}", 10) },
            { @"C:\Test\File11.txt", MakeFileData(i => $"File 11, Line {i}", 10, Encoding.ASCII) },
            { @"C:\Test\File12.txt", MakeFileData(i => $"File 12, Line {i}", 10) },
            { @"C:\Test\File21.txt", MakeFileData(i => $"File 21, Line {i}\0", 10) },
        });

    private MockFileData MakeFileData(Func<int, string> generator, int count, Encoding? encoding = null)
    {
        var lines = Enumerable.Range(1, count).Select(generator);
        var bytes = (encoding ?? Encoding.UTF8).GetBytes(string.Join(Environment.NewLine, lines));
        return new MockFileData(bytes);
    }
}