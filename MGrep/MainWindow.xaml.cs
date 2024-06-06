using System;
using System.Linq;
using System.Windows;

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
}