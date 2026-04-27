namespace MGrep;

/// <summary>
/// Abstracts UI dialogs so the view model can be tested without a real UI.
/// </summary>
public interface IDialogService
{
    /// <summary>Opens the file at <paramref name="match"/> in the configured editor.</summary>
    void OpenEditor(Match match);

    /// <summary>
    /// Prompts the user to choose a file path for export.
    /// Returns <see langword="true"/> and sets <paramref name="fileName"/> when the user confirms.
    /// </summary>
    bool TrySelectFile(out string fileName);

    /// <summary>
    /// Prompts the user to choose a folder.
    /// Returns <see langword="true"/> and sets <paramref name="folderName"/> when the user confirms.
    /// </summary>
    bool TrySelectFolder(out string folderName);
}
