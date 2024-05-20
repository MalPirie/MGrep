using System.Windows;

namespace MGrep;

public sealed class WindowOptions
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public WindowState State { get; set; } = WindowState.Normal;
}