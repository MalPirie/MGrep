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
        if (options.Width > 0 && options.Height > 0)
        {
            Left = options.Left;
            Top = options.Top;
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