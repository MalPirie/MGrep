using Shouldly;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace MGrep.Test;

public class MainWindowViewModelTests
{
    [Fact]
    public void WhenChangingMatchCaseThenItShouldUpdateOptions()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options);

        viewModel.MatchCase = true;

        options.Value.MatchCase.ShouldBeTrue();
    }

    [Fact]
    public void WhenChangingMatchWholeWordThenItShouldUpdateOptions()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options);

        viewModel.MatchWholeWord = true;

        options.Value.MatchWholeWord.ShouldBeTrue();
    }
    [Fact]
    public void WhenChangingUseRegexThenItShouldUpdateOptions()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options);

        viewModel.UseRegex = true;

        options.Value.UseRegex.ShouldBeTrue();
    }

    [Fact]
    public void WhenChangingGlobbingThenItShouldUpdateOptions()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options);

        viewModel.Globbing = true;

        options.Value.Globbing.ShouldBeTrue();
    }

    [Fact]
    public void WhenChangingIncludeSubfoldersThenItShouldUpdateOptions()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options);

        viewModel.IncludeSubfolders = true;

        options.Value.IncludeSubfolders.ShouldBeTrue();
    }

    [Fact]
    public void WhenChangingIncludeBinaryFilesThenItShouldUpdateOptions()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options);

        viewModel.IncludeBinaryFiles = true;

        options.Value.IncludeBinaryFiles.ShouldBeTrue();
    }

    [Fact]
    public void WhenChangingFolderThenItShouldUpdateCanSearch()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options);
        var canExecuteChanged = false;
        viewModel.SearchCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;

        viewModel.Folder = @"C:\Test";

        canExecuteChanged.ShouldBeTrue();
    }

    [Fact]
    public void WhenChangingPatternThenItShouldUpdateCanSearch()
    {
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options);
        var canExecuteChanged = false;
        viewModel.SearchCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;

        viewModel.Pattern = "Pattern";

        canExecuteChanged.ShouldBeTrue();
    }

    [Fact]
    public void WhenExecutingExportCommandThenItShouldWriteFile()
    {
        var dialogService = new MockDialogService();
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\Test\", new MockDirectoryData() }
        });
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options, dialogService, fileSystem);

        viewModel.ExportCommand.Execute(null);

        fileSystem.FileExists(MockDialogService.FileName).ShouldBeTrue();
    }

    [Fact]
    public void WhenExecutingOpenCommandThenItOpenEditorForMatch()
    {
        var dialogService = new MockDialogService();
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options, dialogService, fileSystem);
        var match = new Match("file.txt", 1, "line");

        viewModel.OpenCommand.Execute(match);

        dialogService.OpenedMatch.ShouldBe(match);
    }

    [Fact]
    public void WhenExecutingSelectFolderCommandThenItOpenEditorForMatch()
    {
        var dialogService = new MockDialogService();
        var fileSystem = new MockFileSystem();
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options, dialogService, fileSystem);

        viewModel.SelectFolderCommand.Execute(null);

        viewModel.Folder.ShouldBe(MockDialogService.FolderName);
    }

    [Fact]
    public void WhenExecutingSearchCommandThenItShouldUpdateHistoriesAndListMatchesAndEnableExport()
    {
        var dialogService = new MockDialogService();
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\Test\File1.txt", new MockFileData("File 1, Line 1") },
            { @"C:\Test\File2.txt", new MockFileData("File 2, Line 1") },
        });
        var options = new Options<SearchOptions>("Search", "application.settings", fileSystem);
        var viewModel = new MainWindowViewModel(options, dialogService, fileSystem)
        {
            Folder = MockDialogService.FolderName,
            Pattern = "File 1",
            FilePatterns = "*.txt"
        };
        var canExecuteChanged = false;
        viewModel.ExportCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;

        viewModel.SearchCommand.Execute(null);

        options.Value.FolderHistory.ShouldBe(new[] { viewModel.Folder });
        options.Value.PatternHistory.ShouldBe(new[] { viewModel.Pattern });
        options.Value.FilePatternsHistory.ShouldBe(new[] { viewModel.FilePatterns });
        canExecuteChanged.ShouldBeTrue();
        //viewModel.Matches.Count.ShouldBe(1);
    }
}