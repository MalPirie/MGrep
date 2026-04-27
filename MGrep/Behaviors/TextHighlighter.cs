using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MGrep;

/// <summary>
/// Attached property that renders a <see cref="Match"/> inside a <see cref="TextBlock"/>,
/// bolding and highlighting every matched span.
/// </summary>
public static class TextHighlighter
{
    /// <summary>
    /// Identifies the <c>Match</c> attached property.
    /// Setting it on a <see cref="TextBlock"/> rebuilds the inline run collection.
    /// </summary>
    public static readonly DependencyProperty MatchProperty = DependencyProperty.RegisterAttached(
        "Match",
        typeof(Match),
        typeof(TextHighlighter),
        new PropertyMetadata(default(Match), OnMatchChanged));

    /// <inheritdoc cref="MatchProperty"/>
    public static void SetMatch(TextBlock element, Match value) => element.SetValue(MatchProperty, value);

    /// <inheritdoc cref="MatchProperty"/>
    public static Match GetMatch(TextBlock element) => (Match)element.GetValue(MatchProperty);

    private static void OnMatchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb)
        {
            return;
        }

        tb.Inlines.Clear();

        if (e.NewValue is not Match match || match.Text is null)
        {
            return;
        }

        var text = match.Text;
        var spans = match.Spans ?? [];

        if (spans.Length == 0)
        {
            tb.Inlines.Add(new Run(text));
            return;
        }

        var pos = 0;
        foreach (var (start, length) in spans.OrderBy(s => s.Start))
        {
            if (start > pos)
            {
                tb.Inlines.Add(new Run(text[pos..start]));
            }

            var highlight = new Run(text[start..(start + length)]) { FontWeight = FontWeights.SemiBold };
            highlight.SetResourceReference(TextElement.BackgroundProperty, "BrushMatchHighlightBackground");
            highlight.SetResourceReference(TextElement.ForegroundProperty, "BrushMatchHighlightForeground");
            tb.Inlines.Add(highlight);

            pos = start + length;
        }

        if (pos < text.Length)
        {
            tb.Inlines.Add(new Run(text[pos..]));
        }
    }
}
