using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MGrep;

/// <summary>
/// Represents a physical display monitor and exposes its bounds and working area
/// in WPF device-independent pixels.
/// </summary>
/// <remarks>
/// Uses <c>EnumDisplayMonitors</c> / <c>GetMonitorInfo</c> P/Invoke because
/// WPF's <c>SystemParameters</c> only reports the primary monitor.
/// </remarks>
public class Monitor
{
    #region P/Invoke declarations

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [ResourceExposure(ResourceScope.None)]
    private static extern bool GetMonitorInfo(HandleRef hMonitor, [In, Out] MonitorInfoEx info);

    [DllImport("user32.dll", ExactSpelling = true)]
    [ResourceExposure(ResourceScope.None)]
    private static extern bool EnumDisplayMonitors(HandleRef hDC, IntPtr rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
    private class MonitorInfoEx
    {
        internal int cbSize = Marshal.SizeOf(typeof(MonitorInfoEx));
        internal Rect rcMonitor = new Rect();
        internal Rect rcWork = new Rect();
        internal int dwFlags = 0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal char[] szDevice = new char[32];
    }

    private const int MonitorInfoOfPrimary = 0x00000001;

    #endregion

    /// <summary>A null <see cref="HandleRef"/> used as an empty DC parameter.</summary>
    public static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

    /// <summary>Gets the full bounding rectangle of the monitor in physical pixels.</summary>
    public System.Windows.Rect Bounds { get; private set; }

    /// <summary>Gets the working area (excluding taskbar) of the monitor in physical pixels.</summary>
    public System.Windows.Rect WorkingArea { get; private set; }

    /// <summary>Gets the device name of the monitor (e.g. <c>\\.\DISPLAY1</c>).</summary>
    public string Name { get; private set; }

    /// <summary>Gets a value indicating whether this is the primary monitor.</summary>
    public bool IsPrimary { get; private set; }

    private Monitor(IntPtr monitor, IntPtr hdc)
    {
        var info = new MonitorInfoEx();
        GetMonitorInfo(new HandleRef(null, monitor), info);
        Bounds = new System.Windows.Rect(
                    info.rcMonitor.left, info.rcMonitor.top,
                    info.rcMonitor.right - info.rcMonitor.left,
                    info.rcMonitor.bottom - info.rcMonitor.top);
        WorkingArea = new System.Windows.Rect(
                    info.rcWork.left, info.rcWork.top,
                    info.rcWork.right - info.rcWork.left,
                    info.rcWork.bottom - info.rcWork.top);
        IsPrimary = ((info.dwFlags & MonitorInfoOfPrimary) != 0);
        Name = new string(info.szDevice).TrimEnd((char)0);
    }

    /// <summary>Enumerates all currently connected monitors.</summary>
    public static IEnumerable<Monitor> AllMonitors
    {
        get
        {
            var closure = new MonitorEnumCallback();
            var proc = new MonitorEnumProc(closure.Callback);
            EnumDisplayMonitors(NullHandleRef, IntPtr.Zero, proc, IntPtr.Zero);
            return closure.Monitors.Cast<Monitor>();
        }
    }

    private class MonitorEnumCallback
    {
        public ArrayList Monitors { get; } = new();

        public bool Callback(IntPtr monitor, IntPtr hDC, IntPtr lprcMonitor, IntPtr lParam)
        {
            Monitors.Add(new Monitor(monitor, hDC));
            return true;
        }
    }
}
