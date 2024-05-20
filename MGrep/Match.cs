using System.IO;

namespace MGrep;

public readonly record struct Match(string FileName, int LineNumber, string Text)
{
    public string Name => Path.GetFileName(FileName);
}