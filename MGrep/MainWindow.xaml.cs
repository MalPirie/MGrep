using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

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

    // ── Window chrome ─────────────────────────────────────────────────────

    private IntPtr _hwnd;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;

        UpdateCornerPreference();
        StateChanged += (_, _) => UpdateCornerPreference();

        HwndSource.FromHwnd(_hwnd).AddHook(HookProc);
    }

    // Square corners when maximised (window fills the screen edge-to-edge);
    // rounded corners when in the normal restored state (Windows 11 only).
    private void UpdateCornerPreference()
    {
        int pref = WindowState == WindowState.Maximized
            ? 1  // DWMWCP_DONOTROUND
            : 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(_hwnd, 33, ref pref, sizeof(int));
    }

    private IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case 0x0086: // WM_NCACTIVATE — suppress grey border flash when the window
            {           //   gains or loses focus.  Returning 1 tells Windows the NC
                        //   area is already repainted, preventing the default repaint.
                handled = true;
                return new IntPtr(1);
            }

            case 0x0083: // WM_NCCALCSIZE — suppress the NC frame entirely so the
            {           //   client area equals the full window rect in all states.
                        //   WM_GETMINMAXINFO therefore targets the work area directly
                        //   with no invisible-border offset needed.
                if (wParam != IntPtr.Zero)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
                break;
            }

            case 0x0084: // WM_NCHITTEST
            {
                // Convert raw screen coordinates (physical pixels) to WPF logical pixels.
                var screenPt = new Point(
                    (short)(lParam.ToInt64() & 0xFFFF),
                    (short)((lParam.ToInt64() >> 16) & 0xFFFF));
                var pt = PointFromScreen(screenPt);

                double w = ActualWidth, h = ActualHeight;
                const double border = 6.0; // resize zone depth (logical px)

                // ── Resize zones — only when the window is not maximised ──
                if (WindowState == WindowState.Normal)
                {
                    bool left   = pt.X < border;
                    bool right  = pt.X >= w - border;
                    bool top    = pt.Y < border;
                    bool bottom = pt.Y >= h - border;

                    if (top    && left)  { handled = true; return (IntPtr)13; } // HTTOPLEFT
                    if (top    && right) { handled = true; return (IntPtr)14; } // HTTOPRIGHT
                    if (bottom && left)  { handled = true; return (IntPtr)16; } // HTBOTTOMLEFT
                    if (bottom && right) { handled = true; return (IntPtr)17; } // HTBOTTOMRIGHT
                    if (top)             { handled = true; return (IntPtr)12; } // HTTOP
                    if (bottom)          { handled = true; return (IntPtr)15; } // HTBOTTOM
                    if (left)            { handled = true; return (IntPtr)10; } // HTLEFT
                    if (right)           { handled = true; return (IntPtr)11; } // HTRIGHT
                }

                // ── Caption area — the title bar (excluding buttons) ──────
                // Returning HTCAPTION gives native drag, snap, and
                // double-click-maximise without any additional code.
                if (pt.Y >= 0 && pt.Y < TitleBarGrid.ActualHeight)
                {
                    // Walk up the hit-tested element to check for a Button.
                    // If a Button is found, let the event route normally (HTCLIENT).
                    var hit = VisualTreeHelper.HitTest(this, pt);
                    var element = hit?.VisualHit as DependencyObject;
                    while (element != null && element != this)
                    {
                        if (element is Button || element == AppIconImage) break; // → HTCLIENT
                        element = VisualTreeHelper.GetParent(element);
                    }
                    if (element == null || element == this)
                    {
                        handled = true;
                        return (IntPtr)2; // HTCAPTION
                    }
                }
                break;
            }

            case 0x0024: // WM_GETMINMAXINFO — pin maximised size to work area
            {
                if (lParam == IntPtr.Zero) break;
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                var monitor = MonitorFromWindow(_hwnd, 2); // MONITOR_DEFAULTTONEAREST
                if (monitor != IntPtr.Zero)
                {
                    var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
                    GetMonitorInfo(monitor, ref mi);
                    // WM_NCCALCSIZE returns 0 for all states, so client area == window
                    // rect with no NC inset.  Target the work area exactly.
                    mmi.ptMaxPosition.x = mi.rcWork.Left - mi.rcMonitor.Left;
                    mmi.ptMaxPosition.y = mi.rcWork.Top  - mi.rcMonitor.Top;
                    mmi.ptMaxSize.x     = mi.rcWork.Right  - mi.rcWork.Left;
                    mmi.ptMaxSize.y     = mi.rcWork.Bottom - mi.rcWork.Top;
                }
                Marshal.StructureToPtr(mmi, lParam, false);
                handled = true;
                break;
            }
        }
        return IntPtr.Zero;
    }

    // ── SystemCommands ────────────────────────────────────────────────────

    private void CloseWindow   (object s, ExecutedRoutedEventArgs e) => SystemCommands.CloseWindow(this);
    private void MinimizeWindow(object s, ExecutedRoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void MaximizeWindow(object s, ExecutedRoutedEventArgs e) => SystemCommands.MaximizeWindow(this);
    private void RestoreWindow (object s, ExecutedRoutedEventArgs e) => SystemCommands.RestoreWindow(this);

    private void AppIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (e.ClickCount >= 2)
        {
            SystemCommands.CloseWindow(this);
        }
        else
        {
            var icon = (FrameworkElement)sender;
            var point = icon.PointToScreen(new Point(0, icon.ActualHeight));
            SystemCommands.ShowSystemMenu(this, point);
        }
    }

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