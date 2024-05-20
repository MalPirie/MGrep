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

public sealed class Searcher
{
    private readonly IFileSystem fileSystem;
    private readonly bool includeBinaryFiles;
    private readonly Filter filter;
    private readonly IFileFilter fileFilter;

    public Searcher(bool includeBinaryFiles, IFileFilter fileFilter, Filter filter) : 
        this(includeBinaryFiles, fileFilter, filter, new FileSystem())
    {
    }

    public Searcher(bool includeBinaryFiles, IFileFilter fileFilter, Filter filter, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
        this.includeBinaryFiles = includeBinaryFiles;
        this.fileFilter = fileFilter;
        this.filter = filter;
    }

    public async IAsyncEnumerable<List<Match>> SearchAsync(IProgress<SearchProgress> progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var counter = new ProgressCounter();
        var searchBlock = MakeSearchBlock(counter);
        foreach (var file in fileFilter.Execute())
        {
            searchBlock.Post(file);
        }

        searchBlock.Complete();

        using var p = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var pt = p.WaitForNextTickAsync(cancellationToken).AsTask();
        await using var i = searchBlock.ReceiveAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        var it = i.MoveNextAsync().AsTask();
        while (!cancellationToken.IsCancellationRequested)
        {
            var t = await Task.WhenAny(it, pt).ConfigureAwait(false);
            if (t == it && it.IsCompletedSuccessfully)
            {
                if (it.Result == false)
                {
                    break;
                }
                
                yield return i.Current;
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
    }

    private TransformBlock<string, List<Match>> MakeSearchBlock(ProgressCounter counter) =>
        new(file =>
        {
            var matches = new List<Match>();
            counter.IncrementFileCount();
            if (IsTextFile(file, out var encoding) || includeBinaryFiles)
            {
                var lineCount = 0;
                var matchedAtLeastOnce = false;
                foreach (var line in fileSystem.File.ReadLines(file, encoding))
                {
                    lineCount++;
                    if (filter.IsMatch(line))
                    {
                        counter.IncrementMatchCount();
                        if (!matchedAtLeastOnce)
                        {
                            counter.IncrementFileMatchCount();
                            matchedAtLeastOnce = true;
                        }

                        matches.Add(new Match(file, lineCount, line));
                    }
                }
            }
            else
            {
                counter.IncrementFileIgnoreCount();
            }

            return matches;
        }, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        });

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

    private sealed class ProgressCounter
    {
        private readonly DateTime started = DateTime.UtcNow;

        private int fileCount;
        private int fileIgnoreCount;
        private int fileMatchCount;
        private int matchCount;
        private SearchState state = SearchState.Searching;
        private DateTime completed = DateTime.MinValue;

        public void IncrementFileCount() => Interlocked.Increment(ref fileCount);
        public void IncrementFileIgnoreCount() => Interlocked.Increment(ref fileIgnoreCount);
        public void IncrementFileMatchCount() => Interlocked.Increment(ref fileMatchCount);
        public void IncrementMatchCount() => Interlocked.Increment(ref matchCount);

        public void SetState(SearchState newState)
        {
            state = newState;
            if (state == SearchState.Completed)
            {
                completed = DateTime.UtcNow;
            }
        }

        public SearchProgress ForProgress()  => 
            new(fileCount, fileIgnoreCount, fileMatchCount, matchCount, state, (completed == DateTime.MinValue ? DateTime.UtcNow : completed) - started);
    }
}