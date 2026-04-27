using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MGrep;

/// <summary>
/// Searches a filtered set of files for lines that match a <see cref="Filter"/>
/// and streams batched results back to the caller as an async enumerable.
/// </summary>
/// <remarks>
/// Internally uses TPL Dataflow:
/// <list type="bullet">
///   <item><description>
///     A <see cref="TransformManyBlock{TInput,TOutput}"/> expands the <see cref="IFileFilter"/>
///     into individual file paths (single-threaded).
///   </description></item>
///   <item><description>
///     A <see cref="TransformBlock{TInput,TOutput}"/> processes each file in parallel
///     (up to <see cref="Environment.ProcessorCount"/> concurrent workers).
///   </description></item>
/// </list>
/// A <see cref="PeriodicTimer"/> fires every second to emit progress reports while the
/// pipeline is running.
/// </remarks>
public sealed class Searcher
{
    private readonly IFileSystem fileSystem;
    private readonly bool includeBinaryFiles;
    private readonly Filter filter;
    private readonly IFileFilter fileFilter;

    /// <param name="includeBinaryFiles">
    ///   When <see langword="true"/>, files that do not appear to be text are still searched.
    /// </param>
    public Searcher(bool includeBinaryFiles, IFileFilter fileFilter, Filter filter) :
        this(includeBinaryFiles, fileFilter, filter, new FileSystem())
    {
    }

    /// <summary>Overload that accepts an injectable <see cref="IFileSystem"/> for testing.</summary>
    public Searcher(bool includeBinaryFiles, IFileFilter fileFilter, Filter filter, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
        this.includeBinaryFiles = includeBinaryFiles;
        this.fileFilter = fileFilter;
        this.filter = filter;
    }

    /// <summary>
    /// Returns the start index and length of every match within <paramref name="line"/>,
    /// respecting the same case, whole-word, and regex options used during the search.
    /// </summary>
    public (int Start, int Length)[] GetMatchSpans(string line) => filter.GetMatchSpans(line);

    /// <summary>
    /// Asynchronously searches all files accepted by the <see cref="IFileFilter"/>,
    /// yielding batches of <see cref="Match"/> items as they are found.
    /// Progress is reported approximately once per second via <paramref name="progress"/>.
    /// </summary>
    public async IAsyncEnumerable<List<Match>> SearchAsync(
        IProgress<SearchProgress> progress,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var counter = new ProgressCounter();
        var searchBlock = MakeSearchBlock(counter);

        // Feed file paths from the filter into the search block.
        var listBlock = new TransformManyBlock<IFileFilter, string>(ExecuteFileFilter);
        listBlock.LinkTo(searchBlock, new DataflowLinkOptions { PropagateCompletion = true });
        listBlock.Post(fileFilter);
        listBlock.Complete();

        using var p = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var pt = p.WaitForNextTickAsync(cancellationToken).AsTask();

        await using var i = searchBlock.ReceiveAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        var it = i.MoveNextAsync().AsTask();

        // Race the next batch against the next periodic tick — whichever arrives first is handled.
        while (!cancellationToken.IsCancellationRequested)
        {
            var t = await Task.WhenAny(it, pt).ConfigureAwait(false);
            if (t == it && it.IsCompletedSuccessfully)
            {
                if (it.Result == false)
                {
                    break;
                }

                if (i.Current.Count > 0)
                {
                    yield return i.Current;
                }

                it = i.MoveNextAsync().AsTask();
            }
            else if (t == pt && pt.IsCompletedSuccessfully)
            {
                progress.Report(counter.ForProgress());
                pt = p.WaitForNextTickAsync(cancellationToken).AsTask();
            }
        }

        counter.SetState(cancellationToken.IsCancellationRequested ? SearchState.Cancelled : SearchState.Completed);
        progress.Report(counter.ForProgress());

        // If MoveNextAsync is still in-flight (e.g. after cancellation), wait for it to finish
        // before the async enumerator is disposed — otherwise DisposeAsync throws NotSupportedException.
        await it;
    }

    private static IEnumerable<string> ExecuteFileFilter(IFileFilter fileFilter)
    {
        foreach (var file in fileFilter.Execute())
        {
            yield return file;
        }
    }

    /// <summary>
    /// Builds the parallel search block that reads each file, detects encoding, and
    /// returns a list of matching lines (may be empty for files with no matches).
    /// </summary>
    private TransformBlock<string, List<Match>> MakeSearchBlock(ProgressCounter counter) =>
        new(file =>
        {
            var matches = new List<Match>();
            try
            {
                counter.IncrementFileCount();
                if (IsTextFile(file, out var encoding) || includeBinaryFiles)
                {
                    var lineCount = 0;
                    var matchedAtLeastOnce = false;
                    foreach (var line in fileSystem.File.ReadLines(file, encoding))
                    {
                        lineCount++;
                        var spans = filter.GetMatchSpans(line);
                        if (spans.Length > 0)
                        {
                            counter.IncrementMatchCount();
                            if (!matchedAtLeastOnce)
                            {
                                counter.IncrementFileMatchCount();
                                matchedAtLeastOnce = true;
                            }

                            matches.Add(new Match(file, lineCount, line) { Spans = spans });
                        }
                    }
                }
                else
                {
                    counter.IncrementFileIgnoreCount();
                }
            }
            catch
            {
                counter.IncrementErrorCount();
            }

            return matches;
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        });

    /// <summary>
    /// Determines whether <paramref name="path"/> is a text file by inspecting its BOM
    /// and checking for null bytes (binary indicator).
    /// Sets <paramref name="encoding"/> to the detected encoding (defaults to system default).
    /// </summary>
    private bool IsTextFile(string path, out Encoding encoding)
    {
        encoding = Encoding.Default;

        var buffer = new byte[1000];
        var stream = fileSystem.File.OpenRead(path);
        var length = stream.Read(buffer, 0, buffer.Length);
        stream.Close();

        if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
        {
            encoding = Encoding.UTF8;
        }
        else if (buffer[0] == 0xfe && buffer[1] == 0xff)
        {
            encoding = Encoding.Unicode;
        }
        else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
        {
            encoding = Encoding.UTF32;
        }
        else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
        {
#pragma warning disable SYSLIB0001
            encoding = Encoding.UTF7;
#pragma warning restore SYSLIB0001
        }

        return !Equals(encoding, Encoding.Default) || buffer[..length].All(b => b != 0x00);
    }

    /// <summary>Thread-safe counters used to build <see cref="SearchProgress"/> snapshots.</summary>
    private sealed class ProgressCounter
    {
        private readonly DateTime started = DateTime.UtcNow;

        private int fileCount;
        private int fileIgnoreCount;
        private int fileMatchCount;
        private int errorCount;
        private int matchCount;
        private SearchState state = SearchState.Searching;
        private DateTime completed = DateTime.MinValue;

        public void IncrementFileCount()       => Interlocked.Increment(ref fileCount);
        public void IncrementFileIgnoreCount() => Interlocked.Increment(ref fileIgnoreCount);
        public void IncrementFileMatchCount()  => Interlocked.Increment(ref fileMatchCount);
        public void IncrementErrorCount()      => Interlocked.Increment(ref errorCount);
        public void IncrementMatchCount()      => Interlocked.Increment(ref matchCount);

        public void SetState(SearchState newState)
        {
            state = newState;
            if (state == SearchState.Completed)
            {
                completed = DateTime.UtcNow;
            }
        }

        public SearchProgress ForProgress() =>
            new(fileCount, fileIgnoreCount, fileMatchCount, errorCount, matchCount, state,
                (completed == DateTime.MinValue ? DateTime.UtcNow : completed) - started);
    }
}
