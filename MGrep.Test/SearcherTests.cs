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