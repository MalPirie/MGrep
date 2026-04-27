using System.Diagnostics;

namespace MGrep;

/// <summary>
/// Default implementation of <see cref="IDialogService"/>.
/// Opens VS Code (with fallback to Notepad) and uses Win32 dialogs for file/folder selection.
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc/>
    public void OpenEditor(Match match)
    {
        var pi = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "code",
            Arguments = $"--reuse-windows --goto \"{match.FileName}:{match.LineNumber}\"",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            Process.Start(pi);
        }
        catch
        {
            // VS Code not available — fall back to Notepad.
            pi.WindowStyle = ProcessWindowStyle.Normal;
            pi.FileName = "notepad.exe";
            pi.Arguments = match.FileName;
            Process.Start(pi);
        }
    }

    /// <inheritdoc/>
    public bool TrySelectFile(out string fileName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "Export",
            DefaultExt = ".log",
            Filter = "Log files (.log)|*.log"
        };

        if (dialog.ShowDialog() == true)
        {
            fileName = dialog.FileName;
            return true;
        }

        fileName = string.Empty;
        return false;
    }

    /// <inheritdoc/>
    public bool TrySelectFolder(out string folderName)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Multiselect = false,
            Title = "Select a Starting Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            folderName = dialog.FolderName;
            return true;
        }

        folderName = string.Empty;
        return false;
    }
}
