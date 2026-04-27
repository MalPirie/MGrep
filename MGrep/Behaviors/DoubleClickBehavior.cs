using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MGrep;

/// <summary>
/// Attached behaviour that executes an <see cref="ICommand"/> when the user double-clicks
/// an item in a <see cref="Selector"/> (e.g. <see cref="System.Windows.Controls.ListView"/>).
/// The command parameter is the data item that was double-clicked.
/// </summary>
public static class DoubleClickBehavior
{
    /// <summary>
    /// Identifies the <c>Command</c> attached property.
    /// Attach to any <see cref="Selector"/> to enable double-click-to-open behaviour.
    /// </summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(DoubleClickBehavior),
        new FrameworkPropertyMetadata(null, OnCommandChanged));

    /// <inheritdoc cref="CommandProperty"/>
    public static ICommand? GetCommand(DependencyObject d) => (ICommand?)d.GetValue(CommandProperty);

    /// <inheritdoc cref="CommandProperty"/>
    public static void SetCommand(DependencyObject d, ICommand? value) => d.SetValue(CommandProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Selector selector)
        {
            selector.MouseDoubleClick -= OnMouseDoubleClick;
            if (e.NewValue is ICommand)
            {
                selector.MouseDoubleClick += OnMouseDoubleClick;
            }
        }
    }

    private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ItemsControl control || e.OriginalSource is not DependencyObject originalSender)
        {
            return;
        }

        // Resolve which item container (row) was clicked.
        var container = ItemsControl.ContainerFromElement(control, originalSender);
        if (container == null || container == DependencyProperty.UnsetValue)
        {
            return;
        }

        var activatedItem = control.ItemContainerGenerator.ItemFromContainer(container);
        if (activatedItem != null)
        {
            var command = (ICommand)control.GetValue(CommandProperty);
            if (command != null && command.CanExecute(activatedItem))
            {
                command.Execute(activatedItem);
            }
        }
    }
}
