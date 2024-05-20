using System.Diagnostics;

namespace MGrep;

public interface IDialogService
{
    void OpenEditor(Match match);
    bool TrySelectFile(out string fileName);
    bool TrySelectFolder(out string folderName);
}

public  class DialogService : IDialogService
{
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
            pi.WindowStyle = ProcessWindowStyle.Normal;
            pi.FileName = "notepad.exe";
            pi.Arguments = match.FileName;
            Process.Start(pi);
        }
    }

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