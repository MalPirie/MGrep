using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MGrep;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel, Options<WindowOptions> options)
    {
        InitializeComponent();

        ApplyOptions(options.Value);
        Closing += (_, _) => options.Update(UpdateOptions);

        DataContext = viewModel;
    }

    // ── Rounded corners + maximize fix ───────────────────────────────────

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;

        // Ask DWM to apply Windows 11 rounded corners (no-op on Windows 10).
        int rounded = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(hwnd, 33, ref rounded, sizeof(int));

        // Fix maximize: WM_GETMINMAXINFO must constrain the window to the work area
        // of whichever monitor the window is on, so it doesn't extend behind the taskbar.
        HwndSource.FromHwnd(hwnd).AddHook(HookProc);
    }

    private static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0024 && lParam != IntPtr.Zero) // WM_GETMINMAXINFO
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            var monitor = MonitorFromWindow(hwnd, 2); // MONITOR_DEFAULTTONEAREST
            if (monitor != IntPtr.Zero)
            {
                var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
                GetMonitorInfo(monitor, ref mi);
                // ptMaxPosition is relative to the monitor's top-left corner.
                mmi.ptMaxPosition.x = mi.rcWork.Left - mi.rcMonitor.Left;
                mmi.ptMaxPosition.y = mi.rcWork.Top  - mi.rcMonitor.Top;
                mmi.ptMaxSize.x     = mi.rcWork.Right  - mi.rcWork.Left;
                mmi.ptMaxSize.y     = mi.rcWork.Bottom - mi.rcWork.Top;
            }
            Marshal.StructureToPtr(mmi, lParam, true);
            handled = true;
        }
        return IntPtr.Zero;
    }

    // ── SystemCommands ────────────────────────────────────────────────────

    private void CloseWindow   (object s, ExecutedRoutedEventArgs e) => SystemCommands.CloseWindow(this);
    private void MinimizeWindow(object s, ExecutedRoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void MaximizeWindow(object s, ExecutedRoutedEventArgs e) => SystemCommands.MaximizeWindow(this);
    private void RestoreWindow (object s, ExecutedRoutedEventArgs e) => SystemCommands.RestoreWindow(this);

    // ── Window options ────────────────────────────────────────────────────

    private void ApplyOptions(WindowOptions options)
    {
        var monitors = Monitor.AllMonitors.ToArray();
        if (!monitors.Any(monitor => monitor.Bounds.Contains(options.Left, options.Top)))
        {
            var primaryMonitor = monitors.Single(monitor => monitor.IsPrimary);
            Left = Math.Max(0, (primaryMonitor.Bounds.Width - options.Width) / 2);
            Top = Math.Max(0, (primaryMonitor.Bounds.Height - options.Height) / 2);
        }
        else
        {
            Left = options.Left;
            Top = options.Top;
        }

        if (options.Width > 0 && options.Height > 0)
        {
            Width = options.Width;
            Height = options.Height;
        }

        WindowState = options.State;
    }

    private void UpdateOptions(WindowOptions options)
    {
        options.Left = (int)Left;
        options.Top = (int)Top;
        options.Width = (int)Width;
        options.Height = (int)Height;
        options.State = WindowState;
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────

    [DllImport("dwmapi")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor, rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
    }
}