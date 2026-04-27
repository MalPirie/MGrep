using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MGrep;

/// <summary>
/// View model for <see cref="MainWindow"/>.
/// Exposes search parameters, result collection, status text, and commands.
/// Persists user settings via <see cref="Options{T}"/> and delegates
/// UI interactions to <see cref="IDialogService"/>.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    // ── Search input properties ───────────────────────────────────────────

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

    // ── Search option toggles ─────────────────────────────────────────────

    [ObservableProperty] private bool matchCase;
    [ObservableProperty] private bool matchWholeWord;
    [ObservableProperty] private bool useRegex;
    [ObservableProperty] private bool globbing;
    [ObservableProperty] private bool includeSubfolders;
    [ObservableProperty] private bool includeBinaryFiles;

    // ── Search state ──────────────────────────────────────────────────────

    [ObservableProperty] private bool searching;

    [ObservableProperty] private MatchCollection matches = new();

    [ObservableProperty] private string status = "Ready";

    // ── Theme ─────────────────────────────────────────────────────────────

    [ObservableProperty] private bool isDarkMode;

    // ── Dependencies ──────────────────────────────────────────────────────

    private readonly Options<SearchOptions> options;
    private readonly IDialogService dialogService;
    private readonly IFileSystem fileSystem;
    private readonly IThemeService? themeService;

    // ── Constructors ──────────────────────────────────────────────────────

    /// <summary>Convenience constructor used in design-time and simple test scenarios.</summary>
    public MainWindowViewModel(Options<SearchOptions> options)
        : this(options, new DialogService(), new FileSystem(), null)
    {
    }

    /// <summary>Full constructor used at runtime via <see cref="App"/>.</summary>
    public MainWindowViewModel(
        Options<SearchOptions> options,
        IDialogService dialogService,
        IFileSystem fileSystem,
        IThemeService? themeService = null)
    {
        this.options = options;
        this.dialogService = dialogService;
        this.fileSystem = fileSystem;
        this.themeService = themeService;

        FolderHistory = options.Value.FolderHistory;
        Folder = FolderHistory.FirstOrDefault() ?? fileSystem.Directory.GetCurrentDirectory();

        PatternHistory = options.Value.PatternHistory;
        Pattern = PatternHistory.FirstOrDefault() ?? string.Empty;

        FilePatternsHistory = options.Value.FilePatternsHistory;
        FilePatterns = options.Value.UsingFilePatterns
            ? FilePatternsHistory.FirstOrDefault() ?? string.Empty
            : string.Empty;

        MatchCase = options.Value.MatchCase;
        MatchWholeWord = options.Value.MatchWholeWord;
        UseRegex = options.Value.UseRegex;
        Globbing = options.Value.Globbing;
        IncludeSubfolders = options.Value.IncludeSubfolders;
        IncludeBinaryFiles = options.Value.IncludeBinaryFiles;

        if (themeService != null)
        {
            isDarkMode = themeService.IsDarkMode;
            themeService.ThemeChanged += (_, theme) => IsDarkMode = theme == AppTheme.Dark;
        }
    }

    // ── Option-changed callbacks — persist each toggle immediately ────────

    partial void OnMatchCaseChanged(bool value) => options.Update(o => o.MatchCase = value);

    partial void OnMatchWholeWordChanged(bool value) => options.Update(o => o.MatchWholeWord = value);

    partial void OnUseRegexChanged(bool value) => options.Update(o => o.UseRegex = value);

    partial void OnGlobbingChanged(bool value)
    {
        options.Update(o => o.Globbing = value);
        // Globbing and IncludeSubfolders are mutually exclusive.
        if (value)
        {
            IncludeSubfolders = false;
        }
    }

    partial void OnIncludeSubfoldersChanged(bool value)
    {
        options.Update(o => o.IncludeSubfolders = value);
        // Globbing and IncludeSubfolders are mutually exclusive.
        if (value)
        {
            Globbing = false;
        }
    }

    partial void OnIncludeBinaryFilesChanged(bool value) => options.Update(o => o.IncludeBinaryFiles = value);

    // ── Commands ──────────────────────────────────────────────────────────

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

    [RelayCommand]
    private void ToggleTheme() => themeService?.Toggle();

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

    private bool SearchCriteriaAreValid() =>
        !string.IsNullOrWhiteSpace(Folder) && !string.IsNullOrWhiteSpace(Pattern);

    // ── History management ────────────────────────────────────────────────

    private void UpdateHistories()
    {
        UpdateHistory(FolderHistory,       Folder,       nameof(FolderHistory));
        UpdateHistory(PatternHistory,      Pattern,      nameof(PatternHistory));
        UpdateHistory(FilePatternsHistory, FilePatterns, nameof(FilePatternsHistory));

        options.Update(o =>
        {
            o.FolderHistory = FolderHistory;
            o.PatternHistory = PatternHistory;
            o.FilePatternsHistory = FilePatternsHistory;
            o.UsingFilePatterns = FilePatterns != string.Empty;
        });
    }

    /// <summary>
    /// Moves <paramref name="value"/> to position 0 in <paramref name="history"/>, capping
    /// the list at 10 entries.  Does nothing if the value is already at position 0.
    /// </summary>
    private void UpdateHistory(List<string> history, string value, string propertyName)
    {
        var index = history.IndexOf(value);
        if (index == 0)
        {
            return;
        }

        if (index > 0)
        {
            history.RemoveAt(index);
        }
        else if (history.Count == 10)
        {
            history.RemoveAt(history.Count - 1);
        }

        history.Insert(0, value);
        OnPropertyChanged(propertyName);
    }

    // ── Status formatting ─────────────────────────────────────────────────

    private void UpdateStatus(SearchProgress progress)
    {
        var errors = progress.ErrorCount > 0 ? $", {progress.ErrorCount} errors" : string.Empty;
        var elapsed = Math.Round(progress.Elapsed.TotalSeconds, 0, MidpointRounding.AwayFromZero);

        Status = progress.State switch
        {
            SearchState.Searching =>
                $"Searching {progress.FileCount} files, found {progress.MatchCount} matches " +
                $"in {progress.FileMatchCount} files in {elapsed} seconds{errors}...",
            SearchState.Completed =>
                $"Searched {progress.FileCount} files, found {progress.MatchCount} matches " +
                $"in {progress.FileMatchCount} files in {elapsed} seconds{errors}.",
            SearchState.Cancelled => "Search cancelled",
            SearchState.Faulted   => "Oops",
            _                     => throw new ArgumentOutOfRangeException()
        };
    }
}
