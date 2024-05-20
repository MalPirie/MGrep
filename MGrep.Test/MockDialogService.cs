namespace MGrep.Test;

internal class MockDialogService : IDialogService
{
    public const string FileName = @"C:\Test\Export.txt";
    public const string FolderName = @"C:\Test\";

    public Match OpenedMatch { get; private set; }

    public void OpenEditor(Match match)
    {
        OpenedMatch = match;
    }

    public bool TrySelectFile(out string fileName)
    {
        fileName = FileName;
        return true;
    }

    public bool TrySelectFolder(out string folderName)
    {
        folderName = FolderName;
        return true;
    }
}