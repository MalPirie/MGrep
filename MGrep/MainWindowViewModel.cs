using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MGrep;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string folder = Environment.CurrentDirectory;

    [ObservableProperty] private List<string> folderHistory = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string pattern = string.Empty;

    [ObservableProperty] private List<string> patternHistory = new();

    [ObservableProperty] private string filePatterns = string.Empty;

    [ObservableProperty] private List<string> filePatternsHistory = new();
    
    [ObservableProperty] private bool matchCase;

    [ObservableProperty] private bool matchWholeWord;
    
    [ObservableProperty] private bool useRegex;

    [ObservableProperty] private bool globbing;

    [ObservableProperty] private bool includeSubfolders;

    [ObservableProperty] private bool includeBinaryFiles;

    [ObservableProperty] private bool searching;

    [ObservableProperty] private SemiObservableCollection matches = new();

    [ObservableProperty] private string status = "Ready";

    private readonly Options<SearchOptions> options;
    private readonly IDialogService dialogService;
    private readonly IFileSystem fileSystem;

    public MainWindowViewModel(Options<SearchOptions> options) : this(options, new DialogService(), new FileSystem())
    {
    }

    public MainWindowViewModel(Options<SearchOptions> options, IDialogService dialogService, IFileSystem fileSystem)
    {
        this.options = options;
        this.dialogService = dialogService;
        this.fileSystem = fileSystem;

        FolderHistory = options.Value.FolderHistory;
        Folder = FolderHistory.FirstOrDefault() ?? fileSystem.Directory.GetCurrentDirectory();

        PatternHistory = options.Value.PatternHistory;
        Pattern = PatternHistory.FirstOrDefault() ?? string.Empty;

        FilePatternsHistory = options.Value.FilePatternsHistory;
        FilePatterns = options.Value.UsingFilePatterns ? FilePatternsHistory.FirstOrDefault() ?? string.Empty : string.Empty;

        MatchCase = options.Value.MatchCase;
        MatchWholeWord = options.Value.MatchWholeWord;
        UseRegex = options.Value.UseRegex;
        Globbing = options.Value.Globbing;
        IncludeSubfolders = options.Value.IncludeSubfolders;
        IncludeBinaryFiles = options.Value.IncludeBinaryFiles;
    }

    partial void OnMatchCaseChanged(bool value) => options.Update(o => o.MatchCase = value);

    partial void OnMatchWholeWordChanged(bool value) => options.Update(o => o.MatchWholeWord = value);

    partial void OnUseRegexChanged(bool value) => options.Update(o => o.UseRegex = value);

    partial void OnGlobbingChanged(bool value) => options.Update(o => o.Globbing = value);

    partial void OnIncludeSubfoldersChanged(bool value) => options.Update(o => o.IncludeSubfolders = value);

    partial void OnIncludeBinaryFilesChanged(bool value) => options.Update(o => o.IncludeBinaryFiles = value);

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        if (dialogService.TrySelectFile(out var fileName))
        {
            fileSystem.File.WriteAllLines(fileName, Matches.Select(match => match.Text));
        }
    }

    [RelayCommand]
    private void Open(Match match)
    {
        dialogService.OpenEditor(match);
    }

    [RelayCommand]
    private void SelectFolder()
    {
        if (dialogService.TrySelectFolder(out var folderName))
        {
            Folder = folderName;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSearch), IncludeCancelCommand = true)]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        Searching = true;
        Status = "Searching...";
        try
        {
            UpdateHistories();

            var fileFilter = new FileFilter(Folder, Globbing, IncludeSubfolders);
            fileFilter.AddFilePatterns(FilePatterns.Split('|', StringSplitOptions.RemoveEmptyEntries));

            var filter = new Filter(MatchCase, MatchWholeWord, UseRegex, Pattern);
            var searcher = new Searcher(IncludeBinaryFiles, fileFilter, filter);

            Matches.Clear();
            var progress = new Progress<SearchProgress>(UpdateStatus);
            await foreach (var fileMatches in searcher.SearchAsync(progress, cancellationToken))
            {
                Matches.AddRange(fileMatches);
            }
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
        finally
        {
            Searching = false;
            ExportCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanExport() => Matches.Count > 0;

    private bool CanSearch() => !Searching && SearchCriteriaAreValid();

    private bool SearchCriteriaAreValid() => !string.IsNullOrWhiteSpace(Folder) && !string.IsNullOrWhiteSpace(Pattern);

    private void UpdateHistories()
    {
        UpdateFolderHistory();
        UpdatePatternHistory();
        UpdateFilePatternsHistory();

        options.Update(o =>
        {
            o.FolderHistory = FolderHistory;
            o.PatternHistory = PatternHistory;
            o.FilePatternsHistory = FilePatternsHistory;
            o.UsingFilePatterns = FilePatterns != string.Empty;
        });
    }

    private void UpdateFilePatternsHistory()
    {
        var index = FilePatternsHistory.IndexOf(FilePatterns);
        if (index != 0)
        {
            if (index > 0)
            {
                FilePatternsHistory.RemoveAt(index);
            }
            else if (FilePatternsHistory.Count == 10)
            {
                FilePatternsHistory.RemoveAt(FilePatternsHistory.Count - 1);
            }

            FilePatternsHistory.Insert(0, FilePatterns);
            OnPropertyChanged(nameof(FilePatternsHistory));
        }
    }

    private void UpdateFolderHistory()
    {
        var index = FolderHistory.IndexOf(Folder);
        if (index != 0)
        {
            if (index > 0)
            {
                FolderHistory.RemoveAt(index);
            }
            else if (FolderHistory.Count == 10)
            {
                FolderHistory.RemoveAt(FolderHistory.Count - 1);
            }

            FolderHistory.Insert(0, Folder);
            OnPropertyChanged(nameof(FolderHistory));
        }
    }

    private void UpdatePatternHistory()
    {
        var index = PatternHistory.IndexOf(Pattern);
        if (index != 0)
        {
            if (index > 0)
            {
                PatternHistory.RemoveAt(index);
            }
            else if (PatternHistory.Count == 10)
            {
                PatternHistory.RemoveAt(PatternHistory.Count - 1);
            }

            PatternHistory.Insert(0, Pattern);
            OnPropertyChanged(nameof(PatternHistory));
        }
    }

    private void UpdateStatus(SearchProgress progress)
    {
        Status = progress.State switch
        {
            SearchState.Searching =>
                $"Searching {progress.FileCount} files, found {progress.MatchCount} matches in {progress.FileMatchCount} files in {Math.Round(progress.Elapsed.TotalSeconds, 0, MidpointRounding.AwayFromZero)} seconds...",
            SearchState.Completed =>
                $"Searched {progress.FileCount} files, found {progress.MatchCount} matches in {progress.FileMatchCount} files in {Math.Round(progress.Elapsed.TotalSeconds, 0, MidpointRounding.AwayFromZero)} seconds.",
            SearchState.Cancelled => "Search cancelled",
            SearchState.Faulted => "Oops",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}